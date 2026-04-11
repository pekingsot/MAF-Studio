#!/usr/bin/env python3
"""集成测试脚本 - 视频解析功能端到端测试"""

import requests
import time
import json

class VideoParserIntegrationTest:
    BASE_URL = "http://localhost:8080/api"
    
    def setup(self):
        """测试环境准备"""
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'
        })
    
    def test_douyin_full_flow(self):
        """抖音视频完整流程测试"""
        print("▶️  开始抖音视频完整流程测试...")
        
        # 步骤1: 输入链接
        payload = {
            "url": "https://v.douyin.com/ixJ8xLk/",
            "platform": "douyin"
        }
        
        # 步骤2: 调用解析接口
        start_time = time.time()
        response = self.session.post(
            f"{self.BASE_URL}/parse",
            json=payload
        )
        parse_time = time.time() - start_time
        
        assert response.status_code == 200
        data = response.json()
        
        # 步骤3: 验证响应结构
        assert 'success' in data
        assert 'data' in data
        assert data['success'] is True
        
        # 步骤4: 验证视频信息完整性
        video_info = data['data']
        assert 'video_url' in video_info
        assert 'title' in video_info
        assert 'author' in video_info
        assert 'is_watermark_free' is True
        
        # 步骤5: 性能检查
        assert parse_time < 3.0, f"解析超时: {parse_time:.2f}秒"
        
        print(f"✅ 抖音视频解析成功，耗时: {parse_time:.2f}秒")
    
    def test_ad_service_integration(self):
        """广告服务集成测试"""
        print("▶️  开始广告服务集成测试...")
        
        # 模拟首次访问
        user_session = {"user_id": "test_user_001", "ad_watched_today": False}
        
        # 检查是否需要展示广告
        need_ad = not user_session["ad_watched_today"]
        assert need_ad is True
        
        # 模拟观看广告
        ad_response = self.session.post(
            f"{self.BASE_URL}/ad/watch",
            json={
                "user_id": user_session["user_id"],
                "ad_type": "rewarded_video"
            }
        )
        
        assert ad_response.status_code == 200
        result = ad_response.json()
        assert result.get("success") is True
        assert result.get("unlocked") is True
        
        # 更新会话状态
        user_session["ad_watched_today"] = True
        
        # 验证后续请求无需再次观看广告
        check_response = self.session.post(
            f"{self.BASE_URL}/check_access",
            json=user_session
        )
        access_data = check_response.json()
        assert access_data.get("can_use_infinite") is True
        
        print("✅ 广告服务集成测试通过")
    
    def test_link_extraction_accuracy(self):
        """链接提取准确率测试"""
        test_cases = [
            ("https://v.douyin.com/abcdefg/", True),
            ("https://www.douyin.com/video/123456789", True),
            ("https://v.kuaishou.com/xyz123", True),
            ("这不是链接", False),
            ("https://example.com/video.mp4", False),
        ]
        
        for url, should_extract in test_cases:
            response = self.session.post(
                f"{self.BASE_URL}/extract_links",
                json={"text": url}
            )
            data = response.json()
            
            if should_extract:
                assert data['links'] != [], f"应该提取到链接: {url}"
            else:
                assert data['links'] == [] or len(data['links']) == 0, \
                    f"不应提取到链接: {url}"
        
        print("✅ 链接提取准确率测试通过")

if __name__ == "__main__":
    test = VideoParserIntegrationTest()
    test.setup()
    
    try:
        test.test_douyin_full_flow()
        test.test_ad_service_integration()
        test.test_link_extraction_accuracy()
        print("\n🎉 所有集成测试通过!")
    except AssertionError as e:
        print(f"\n❌ 测试失败: {e}")
        exit(1)
    except Exception as e:
        print(f"\n❌ 异常: {e}")
        exit(1)