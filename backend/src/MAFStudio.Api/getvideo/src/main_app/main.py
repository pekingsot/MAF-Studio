# Main Application Entry Point
# Integrates all modules for video parsing, link processing, and ad management

from src.link_parser import parse_link
from src.video_parser import parse_video
from src.ad_manager import AdManager
import config

ad_manager = AdManager.get_instance()

def process_user_input(shared_url):
    """Process user-pasted URL and return video download options"""
    try:
        # Step 1: Parse shared URL to extract video links
        video_urls = parse_link(shared_url)
        
        # Step 2: Process each video URL to get watermark-free HD version
        results = []
        for url in video_urls:
            processed_video = parse_video(url)
            results.append(processed_video)
        
        return results
    except Exception as e:
        print(f"Error processing request: {str(e)}")
        return None

if __name__ == "__main__":
    # Example usage (would normally come from user input)
    test_url = "https://example.com/tiktok/video/12345"
    
    # Check if ad needs to be shown first
    if not ad_manager.ad_shown_today:
        print("Please watch an ad to continue...")
        # In real implementation, this would trigger ad display UI
        # ad_manager.show_ad(lambda: process_user_input(test_url))
        
    # Process the video request
    video_results = process_user_input(test_url)
    if video_results:
        print(f"Found {len(video_results)} video(s) for download")