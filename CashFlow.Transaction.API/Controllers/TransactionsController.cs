using CashFlow.Transaction.Application.Commands.CreateTransaction;
using CashFlow.Transaction.Application.DTOs;
using CashFlow.Transaction.Application.Queries.GetTransactionById;
using CashFlow.Transaction.Application.Queries.GetTransactionsByMerchant;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Transaction.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
        
    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }
        
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var command = CreateTransactionCommand.FromRequest(request);
        var transactionId = await _mediator.Send(command);
            
        return CreatedAtAction(nameof(GetTransaction), new { id = transactionId }, null);
    }
        
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        var query = new GetTransactionByIdQuery { Id = id };
        var transaction = await _mediator.Send(query);
            
        if (transaction == null)
            return NotFound();
            
        return Ok(transaction);
    }
        
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] string merchantId,
        [FromQuery] DateTime? date = null)
    {
        var query = new GetTransactionsByMerchantQuery
        {
            MerchantId = merchantId,
            Date = date
        };
            
        var transactions = await _mediator.Send(query);
        return Ok(transactions);
    }
}