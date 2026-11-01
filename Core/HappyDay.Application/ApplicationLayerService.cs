using FluentValidation;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Features.Commands.Company.CreateCompany;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Mapping;
using HappyDay.Application.Validations.Company;
using HappyDay.Persistance.Security;
using Infrastructure.Services;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace HappyDay.Application;

public static class ApplicationLayerService
{
    public static IServiceCollection AddAplicationLayerServices(this IServiceCollection services)
    {
        var currentAssembly = typeof(ApplicationLayerService).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(currentAssembly));

        services.AddSingleton<IMapper>(new Mapper(MapsterConfiguration.Create()));

        // Tüm validator'ları otomatik olarak ekle
        services.AddValidatorsFromAssembly(currentAssembly);
        services.AddScoped<JwtService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}