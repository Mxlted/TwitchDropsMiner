import math
import unittest
from unittest.mock import MagicMock

from src.models.campaign import DropsCampaign


def _campaign_data(time_based_drops=None):
    return {
        "id": "campaign-1",
        "name": "Empty Campaign",
        "game": {
            "id": "1",
            "name": "Test Game",
            "displayName": "Test Game",
            "boxArtURL": "https://example.test/game-{width}x{height}.jpg",
        },
        "self": {"isAccountConnected": True},
        "accountLinkURL": "https://example.test/link",
        "startAt": "2026-01-01T00:00:00Z",
        "endAt": "2026-01-02T00:00:00Z",
        "status": "ACTIVE",
        "allow": {"channels": [], "isEnabled": True},
        "timeBasedDrops": time_based_drops or [],
    }


class TestCampaignMetrics(unittest.TestCase):
    def test_empty_campaign_metrics_are_safe_defaults(self):
        campaign = DropsCampaign(MagicMock(), _campaign_data(), {})

        self.assertEqual(campaign.total_drops, 0)
        self.assertEqual(campaign.required_minutes, 0)
        self.assertEqual(campaign.remaining_minutes, 0)
        self.assertEqual(campaign.progress, 0.0)
        self.assertEqual(campaign.availability, math.inf)
        self.assertIsNone(campaign.first_drop)


if __name__ == "__main__":
    unittest.main()
