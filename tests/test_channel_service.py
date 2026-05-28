import unittest
from unittest.mock import AsyncMock, MagicMock

from src.models.channel import Channel
from src.services.channel_service import ChannelService


class TestChannelService(unittest.IsolatedAsyncioTestCase):
    async def test_bulk_check_online_accepts_one_shot_iterables(self):
        twitch = MagicMock()
        twitch.gql_request = AsyncMock(
            return_value=[
                {"data": {"user": {"id": "1", "stream": {"id": "stream-1"}}}},
                {"data": {"user": {"id": "2", "stream": {"id": "stream-2"}}}},
            ]
        )
        channels = []
        for channel_id in (1, 2):
            channel = MagicMock(spec=Channel)
            channel.id = channel_id
            channel.stream_gql = {"operationName": f"GetStreamInfo{channel_id}"}
            channels.append(channel)

        await ChannelService(twitch).bulk_check_online(channel for channel in channels)

        twitch.gql_request.assert_awaited_once()
        channels[0].external_update.assert_called_once_with(
            {"id": "1", "stream": {"id": "stream-1"}}, []
        )
        channels[1].external_update.assert_called_once_with(
            {"id": "2", "stream": {"id": "stream-2"}}, []
        )


if __name__ == "__main__":
    unittest.main()
