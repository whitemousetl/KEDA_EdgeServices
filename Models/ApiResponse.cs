using System.Net;

namespace KEDA_EdgeServices.Models;

/// <summary>
/// 通用API响应模型，封装接口返回的标准结构。
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>()
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 提示或错误信息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// •OK (200)：请求成功，返回数据。
    /// •Created(201)：资源创建成功，常用于POST。
    /// •NoContent(204)：请求成功但无返回内容，常用于PUT/DELETE。
    /// •BadRequest(400)：请求参数有误，客户端错误。
    /// •Unauthorized(401)：未认证或认证失败。
    /// •Forbidden(403)：已认证但无权限。
    /// •NotFound(404)：资源不存在。
    /// •Conflict(409)：请求冲突，常用于资源已存在等场景。
    /// •InternalServerError(500)：服务器内部错误。
    /// </summary>
    public HttpStatusCode code { get; set; }

    /// <summary>
    /// 错误详情集合
    /// </summary>
    public IEnumerable<string> Errors { get; set; } = [];

    public string Time { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>
    /// 返回的数据内容
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 生成成功响应,请求成功，返回数据。
    /// </summary>
    /// <param name="data">返回数据</param>
    /// <param name="message">提示信息</param>
    /// <param name="statusCode">HTTP状态码，默认200</param>
    public static ApiResponse<T> Success(string message = "", T? data = default, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            code = statusCode,
            Errors = []
        };
    }

    /// <summary>
    /// 生成失败响应,请求参数有误，客户端错误。
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="statusCode">HTTP状态码，默认400</param>
    /// <param name="errors">错误详情集合</param>
    public static ApiResponse<T> Fial(string message = "", HttpStatusCode statusCode = HttpStatusCode.BadRequest, IEnumerable<string>? errors = default)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message,
            code = statusCode,
            Errors = errors ?? []
        };
    }

    /// <summary>
    /// 生成失败响应,请求参数有误，客户端错误。
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="statusCode">HTTP状态码，默认400</param>
    /// <param name="errors">错误详情集合</param>
    public static ApiResponse<T> FialWithData(string message = "", T? data = default, HttpStatusCode statusCode = HttpStatusCode.BadRequest, IEnumerable<string>? errors = default)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Data = data,
            Message = message,
            code = statusCode,
            Errors = errors ?? []
        };
    }

    /// <summary>
    /// 根据异常生成失败响应,服务器内部错误。
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="statusCode">HTTP状态码，默认500</param>
    public static ApiResponse<T> FromException(Exception ex, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Data = default,
            Message = ex.Message,
            code = statusCode,
            Errors = [ex.ToString()]
        };
    }
}