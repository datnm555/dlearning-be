using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static readonly Error InvalidEmail = Error.Validation(
        "Users.InvalidEmail",
        "Email không hợp lệ.");

    public static readonly Error InvalidUsername = Error.Validation(
        "Users.InvalidUsername",
        "Tên đăng nhập phải dài 3–30 ký tự và chỉ gồm chữ cái hoặc chữ số.");

    public static readonly Error InvalidDisplayName = Error.Validation(
        "Users.InvalidDisplayName",
        "Tên hiển thị không được để trống.");

    public static readonly Error PasswordTooShort = Error.Validation(
        "Users.PasswordTooShort",
        "Mật khẩu phải có ít nhất 8 ký tự.");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "Email này đã được đăng ký.");

    public static readonly Error UsernameNotUnique = Error.Conflict(
        "Users.UsernameNotUnique",
        "Tên đăng nhập này đã tồn tại.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Users.InvalidCredentials",
        "Thông tin đăng nhập không đúng.");
}
