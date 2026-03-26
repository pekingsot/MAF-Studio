declare module 'event-source-polyfill' {
  export interface EventSourcePolyfillInit {
    headers?: Record<string, string>;
    method?: string;
    body?: string;
    withCredentials?: boolean;
  }

  export class EventSourcePolyfill extends EventTarget {
    constructor(url: string, init?: EventSourcePolyfillInit);
    readonly readyState: number;
    readonly url: string;
    onopen: ((this: EventSource, ev: Event) => any) | null;
    onmessage: ((this: EventSource, ev: MessageEvent) => any) | null;
    onerror: ((this: EventSource, ev: Event) => any) | null;
    addEventListener(type: string, listener: (this: EventSource, event: MessageEvent) => any): void;
    addEventListener(type: string, listener: (this: EventSource, event: Event) => any): void;
    close(): void;
  }
}
