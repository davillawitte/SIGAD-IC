namespace TemplateSistema.Application.Abstractions;

public interface IPasswordHasherService
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}
