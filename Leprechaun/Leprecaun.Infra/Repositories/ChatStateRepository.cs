using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Leprecaun.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class ChatStateRepository : IChatStateRepository
{
    private readonly LeprechaunDbContext _context;

    public ChatStateRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public async Task<ChatState?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken)
    {
        return await _context.ChatStates
            .FirstOrDefaultAsync(c => c.ChatId == chatId, cancellationToken);
    }

    public async Task SaveAsync(ChatState state, CancellationToken cancellationToken)
    {
        var existing = await _context.ChatStates
            .FirstOrDefaultAsync(c => c.ChatId == state.ChatId, cancellationToken);

        if (existing == null)
        {
            await _context.ChatStates.AddAsync(state, cancellationToken);
        }
        else
        {
            existing.State = state.State;
            existing.TempInstitutionId = state.TempInstitutionId;
            existing.TempAmount = state.TempAmount;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearStateAsync(long chatId, CancellationToken cancellationToken)
    {
        var existing = await _context.ChatStates
            .FirstOrDefaultAsync(c => c.ChatId == chatId, cancellationToken);

        if (existing != null)
        {
            existing.State = "Idle";
            existing.TempInstitutionId = null;
            existing.TempAmount = null;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}