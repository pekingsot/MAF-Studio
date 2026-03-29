import { AxiosError } from 'axios';
import { message } from 'antd';

export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, unknown>;
}

export const handleApiError = (error: unknown, fallbackMessage = '操作失败'): string => {
  let errorMessage = fallbackMessage;

  if (error instanceof AxiosError) {
    const responseData = error.response?.data as ApiError | undefined;
    
    if (responseData?.message) {
      errorMessage = responseData.message;
    } else if (error.response) {
      switch (error.response.status) {
        case 400:
          errorMessage = '请求参数错误';
          break;
        case 401:
          errorMessage = '未登录或登录已过期';
          break;
        case 403:
          errorMessage = '权限不足';
          break;
        case 404:
          errorMessage = '请求的资源不存在';
          break;
        case 500:
          errorMessage = '服务器内部错误';
          break;
        default:
          errorMessage = `请求失败 (${error.response.status})`;
      }
    } else if (error.request) {
      errorMessage = '网络连接失败，请检查网络';
    }
  } else if (error instanceof Error) {
    errorMessage = error.message;
  }

  message.error(errorMessage);
  return errorMessage;
};

export const wrapApiCall = async <T>(
  apiCall: () => Promise<T>,
  successMessage?: string
): Promise<T | null> => {
  try {
    const result = await apiCall();
    if (successMessage) {
      message.success(successMessage);
    }
    return result;
  } catch (error) {
    handleApiError(error);
    return null;
  }
};

export const withLoading = async <T>(
  loadingSetter: (loading: boolean) => void,
  apiCall: () => Promise<T>
): Promise<T | null> => {
  try {
    loadingSetter(true);
    return await apiCall();
  } catch (error) {
    handleApiError(error);
    return null;
  } finally {
    loadingSetter(false);
  }
};
