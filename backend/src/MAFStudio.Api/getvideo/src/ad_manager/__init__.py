# Ad Manager Module
# This module handles WeChat mini program ads integration

import wxads
from datetime import datetime, timedelta

class AdManager:
    _instance = None
    
    @staticmethod
    def get_instance():
        if AdManager._instance is None:
            AdManager._instance = AdManager()
        return AdManager._instance

    def __init__(self):
        self.ad_shown_today = False
        self.last_ad_date = None
        # Initialize WeChat ad SDK
        wxads.init(ad_unit_id="YOUR_AD_UNIT_ID")

    def show_ad(self, callback):
        """Show ad and trigger callback after completion"""
        if not self._should_show_ad():
            return False

        # Track ad impression
        self.ad_shown_today = True
        self.last_ad_date = datetime.now()

        # Show rewarded ad
        wxads.show_rewarded_ad(
            ad_id="VIDEO_DOWNLOAD_AD",
            on_complete=callback
        )
        return True

    def _should_show_ad(self):
        """Check if ad should be shown based on daily limit"""
        if self.ad_shown_today:
            return False

        if self.last_ad_date and \
           datetime.now() - self.last_ad_date < timedelta(days=1):
            return False

        return True