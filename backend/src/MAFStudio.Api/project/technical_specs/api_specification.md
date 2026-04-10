# API接口规范

## 1. 视频解析接口
**POST /api/v1/parse**
- 请求参数：
  ```json
  {
    "url": "https://www.douyin.com/video/123456"
  }
  ```
- 响应格式：
  ```json
  {
    "videoUrl": "https://cdn.example.com/video.mp4",
    "thumbnail": "https://cdn.example.com/thumb.jpg",
    "platform": "douyin",
    "quality": "1080p"
  }
  ```

## 2. 广告验证接口
**POST /api/v1/ad-validate**
- 请求参数：
  ```json
  {
    "deviceId": "xxx",
    "adToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
  ```
- 响应格式：
  ```json
  {
    "valid": true,
    "expireTime": "2024-02-20T15:30:00Z"
  }
  ```

## 3. 下载加速接口
**GET /api/v1/accelerate?videoId=xxxx**
- 返回带CDN加速的视频地址