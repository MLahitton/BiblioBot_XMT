from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict


BASE_DIR = Path(__file__).resolve().parents[2]


class Settings(BaseSettings):
    app_name: str = "BiblioBot Chatbot"
    environment: str = "development"

    gemini_api_key: str = ""
    gemini_model: str = "gemini-2.5-flash"

    dotnet_api_base_url: str = "http://localhost:5000"
    chatbot_internal_api_key: str = "dev_internal_key"

    use_mock_dotnet_client: bool = True

    model_config = SettingsConfigDict(
        env_file=BASE_DIR / ".env",
        env_file_encoding="utf-8",
        extra="ignore"
    )

    @classmethod
    def settings_customise_sources(
        cls,
        settings_cls,
        init_settings,
        env_settings,
        dotenv_settings,
        file_secret_settings,
    ):
        return (
            init_settings,
            dotenv_settings,
            env_settings,
            file_secret_settings,
        )


settings = Settings()
