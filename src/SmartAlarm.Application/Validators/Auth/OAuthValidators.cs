using FluentValidation;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Application.Validators.Auth;

/// <summary>
/// Validator para comando de autorização OAuth2
/// </summary>
public class GetOAuthAuthorizationUrlCommandValidator : AbstractValidator<GetOAuthAuthorizationUrlCommand>
{
    public GetOAuthAuthorizationUrlCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .Must(BeValidProvider)
            .WithMessage("Provider must be one of: " + string.Join(", ", ExternalAuthInfo.SupportedProviders.All));

        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .WithMessage("RedirectUri is required")
            .Must(BeValidUri)
            .WithMessage("RedirectUri must be a valid URI");

        RuleFor(x => x.State)
            .MaximumLength(256)
            .WithMessage("State parameter cannot exceed 256 characters");
    }

    private static bool BeValidProvider(string provider)
    {
        return ExternalAuthInfo.SupportedProviders.IsSupported(provider);
    }

    private static bool BeValidUri(string uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator para comando de callback OAuth2
/// </summary>
public class ProcessOAuthCallbackCommandValidator : AbstractValidator<ProcessOAuthCallbackCommand>
{
    public ProcessOAuthCallbackCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .Must(BeValidProvider)
            .WithMessage("Provider must be one of: " + string.Join(", ", ExternalAuthInfo.SupportedProviders.All));

        RuleFor(x => x.AuthorizationCode)
            .NotEmpty()
            .WithMessage("Authorization code is required");

        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .WithMessage("RedirectUri is required")
            .Must(BeValidUri)
            .WithMessage("RedirectUri must be a valid URI");

        RuleFor(x => x.State)
            .MaximumLength(256)
            .WithMessage("State parameter cannot exceed 256 characters");
    }

    private static bool BeValidProvider(string provider)
    {
        return ExternalAuthInfo.SupportedProviders.IsSupported(provider);
    }

    private static bool BeValidUri(string uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator para DTO de request OAuth2
/// </summary>
public class OAuthCallbackRequestDtoValidator : AbstractValidator<OAuthCallbackRequestDto>
{
    public OAuthCallbackRequestDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.Error))
            .WithMessage("Authorization code is required when no error is present");

        RuleFor(x => x.State)
            .MaximumLength(256)
            .WithMessage("State parameter cannot exceed 256 characters");
    }
}