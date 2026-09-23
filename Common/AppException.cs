namespace KtxAI.Api.Common
{
    // Exception nghiệp vụ có kèm mã trạng thái HTTP, để middleware xử lý lỗi
    // trả về đúng status code thay vì luôn là 500 (tiêu chí KT2 #8).
    public class AppException : Exception
    {
        public int StatusCode { get; }

        public AppException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }

        public static AppException NotFound(string message) => new AppException(message, 404);
        public static AppException Conflict(string message) => new AppException(message, 409);
        public static AppException Forbidden(string message) => new AppException(message, 403);
        public static AppException Unauthorized(string message) => new AppException(message, 401);
    }
}
