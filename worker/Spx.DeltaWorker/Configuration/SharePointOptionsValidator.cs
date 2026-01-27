using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Spx.DeltaWorker.Configuration;

public sealed class SharePointOptionsValidator(IOptions<DeltaOptions> deltaOptions)
    : IValidateOptions<SharePointOptions>
{
    private readonly DeltaOptions _deltaOptions = deltaOptions.Value;

    public ValidateOptionsResult Validate(string? name, SharePointOptions options)
    {
        if (!_deltaOptions.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var ok = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);
        if (ok)
        {
            return ValidateOptionsResult.Success;
        }

        var message = string.Join("; ", results.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
        return ValidateOptionsResult.Fail(message);
    }
}