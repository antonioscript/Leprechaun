using Leprechaun.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leprechaun.Domain.Interfaces;
public interface ISupportSuggestionService
{
    Task<SupportSuggestion> CreateAsync(
        long chatId,
        string description,
        CancellationToken cancellationToken = default);

    Task<List<SupportSuggestion>> GetAllAsync(CancellationToken cancellationToken = default);
}