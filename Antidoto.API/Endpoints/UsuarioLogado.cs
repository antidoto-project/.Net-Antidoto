using System.Security.Claims;
using Antidoto.API.Models;

namespace Antidoto.API.Endpoints;

public static class UsuarioLogado
{
    public static Guid Id(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool EhGestor(this ClaimsPrincipal user) => user.IsInRole(Roles.Gestor);
}
