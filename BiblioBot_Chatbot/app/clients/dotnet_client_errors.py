class DotNetApiError(Exception):
    status_code: int | None = None
    error_code = "backend_error"

    def __init__(self, message: str = "Error controlado al consultar ASP.NET Core."):
        super().__init__(message)
        self.message = message

    def to_safe_dict(self) -> dict:
        return {
            "status": "BACKEND_ERROR",
            "mode": "READ_ONLY",
            "errorCode": self.error_code,
            "message": self.message,
        }


class DotNetApiConfigurationError(DotNetApiError):
    error_code = "backend_configuration_error"


class DotNetApiTimeoutError(DotNetApiError):
    status_code = 408
    error_code = "backend_timeout"


class DotNetApiUnauthorizedError(DotNetApiError):
    status_code = 401
    error_code = "backend_unauthorized"


class DotNetApiForbiddenError(DotNetApiError):
    status_code = 403
    error_code = "permission_denied"


class DotNetApiNotFoundError(DotNetApiError):
    status_code = 404
    error_code = "not_found"


class DotNetApiConflictError(DotNetApiError):
    status_code = 409
    error_code = "conflict"


class DotNetApiUnavailableError(DotNetApiError):
    error_code = "backend_unavailable"


class DotNetApiInvalidResponseError(DotNetApiError):
    error_code = "backend_invalid_response"


class DotNetApiBadRequestError(DotNetApiError):
    status_code = 400
    error_code = "bad_request"


class DotNetApiMutationDisabledError(DotNetApiError):
    error_code = "backend_mutation_disabled"

    def __init__(self):
        super().__init__(
            "La mutacion real hacia ASP.NET Core esta deshabilitada por configuracion."
        )
