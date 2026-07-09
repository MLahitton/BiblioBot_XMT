from app.clients.dotnet_api_client import DotNetApiClient
from app.clients.dotnet_client_protocol import DotNetClientProtocol
from app.clients.mock_dotnet_client import MockDotNetClient
from app.core.config import Settings, settings


def get_dotnet_client(app_settings: Settings | None = None) -> DotNetClientProtocol:
    current_settings = app_settings or settings
    if current_settings.use_mock_dotnet_client:
        return MockDotNetClient()
    return DotNetApiClient(current_settings)
