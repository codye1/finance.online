using finance.online.mvc.Models;

namespace finance.online.mvc.Services

{
    public interface ICurrentUserService
    {
        HomeUserModel? GetUser();
    }
}
