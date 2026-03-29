import { useRef, useEffect, useCallback } from 'react';

export const useAbortController = () => {
  const abortControllerRef = useRef<AbortController | null>(null);

  const getAbortSignal = useCallback(() => {
    if (!abortControllerRef.current || abortControllerRef.current.signal.aborted) {
      abortControllerRef.current = new AbortController();
    }
    return abortControllerRef.current.signal;
  }, []);

  const abort = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
  }, []);

  useEffect(() => {
    return () => {
      abort();
    };
  }, [abort]);

  return {
    getAbortSignal,
    abort,
  };
};

export const createCancellableRequest = <T>(
  requestFn: (signal: AbortSignal) => Promise<T>
): {
  promise: Promise<T>;
  cancel: () => void;
} => {
  const abortController = new AbortController();

  const promise = requestFn(abortController.signal).catch((error) => {
    if (error.name === 'AbortError' || error.name === 'CanceledError') {
      console.log('Request was cancelled');
    }
    throw error;
  });

  return {
    promise,
    cancel: () => abortController.abort(),
  };
};

export const isCancelledError = (error: unknown): boolean => {
  return (
    error instanceof Error &&
    (error.name === 'AbortError' || error.name === 'CanceledError')
  );
};
