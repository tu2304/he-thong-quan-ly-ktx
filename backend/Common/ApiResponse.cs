namespace KtxAI.Api.Common
{
    // Bọc mọi response theo một định dạng thống nhất để frontend xử lý
    // thông báo lỗi/thành công một cách nhất quán (tiêu chí KT2 #6, #8).
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
            => new ApiResponse<T> { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new ApiResponse<T> { Success = false, Message = message, Data = default };
    }

    // Kết quả phân trang cho các API tìm kiếm/lọc danh sách
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
