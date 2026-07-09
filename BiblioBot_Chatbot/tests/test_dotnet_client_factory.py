import httpx
import pytest

from app.clients.dotnet_api_client import DotNetApiClient
from app.clients.dotnet_client_errors import DotNetApiError
from app.clients.dotnet_client_factory import get_dotnet_client
from app.clients.mock_dotnet_client import MockDotNetClient
from app.core.config import Settings


def test_factory_selects_mock_when_enabled():
    client = get_dotnet_client(Settings(use_mock_dotnet_client=True))

    assert isinstance(client, MockDotNetClient)


def test_factory_selects_real_client_when_mock_disabled():
    client = get_dotnet_client(
        Settings(use_mock_dotnet_client=False, dotnet_api_base_url="http://dotnet.test")
    )

    assert isinstance(client, DotNetApiClient)


def test_factory_real_mode_requires_valid_base_url():
    with pytest.raises(DotNetApiError):
        get_dotnet_client(Settings(use_mock_dotnet_client=False, dotnet_api_base_url=""))


def test_factory_does_not_make_requests_when_selecting_real_client():
    calls = 0

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(200, json=[])

    DotNetApiClient(
        Settings(use_mock_dotnet_client=False, dotnet_api_base_url="http://dotnet.test"),
        transport=httpx.MockTransport(handler),
    )

    assert calls == 0
