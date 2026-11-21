using System.Globalization;
using System.Text;
using System.Text.Json;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Clinic setting read/update helpers.</summary>
public sealed partial class AdminService
{

    public async Task<IReadOnlyList<ClinicSettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        await db.ClinicSettings.AsNoTracking().OrderBy(x => x.Key)
            .Select(x => new ClinicSettingDto(x.Key, x.ValueJson, x.Description, x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<ClinicSettingDto> UpdateSettingAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default)
    {
        NormalizeJson(request.ValueJson);
        var setting = await db.ClinicSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is null)
        {
            setting = new ClinicSetting { Key = key, ValueJson = request.ValueJson, Description = request.Description };
            db.ClinicSettings.Add(setting);
        }
        else
        {
            setting.ValueJson = request.ValueJson;
            setting.Description = request.Description ?? setting.Description;
        }
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        setting.UpdatedBy = user.RequireId();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admin.setting-updated", nameof(ClinicSetting), key, after: new { key, setting.ValueJson }, cancellationToken: cancellationToken);
        return new ClinicSettingDto(setting.Key, setting.ValueJson, setting.Description, setting.UpdatedAt);
    }

    private static string NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        try
        {
            using var _ = JsonDocument.Parse(json);
            return json;
        }
        catch (JsonException)
        {
            throw new AppConflictException("Value must be valid JSON.");
        }
    }
}
