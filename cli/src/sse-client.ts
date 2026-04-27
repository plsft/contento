import { getConfig } from './config.js';

export interface SSEEvent {
  type: string;
  data: unknown;
}

export interface SSEProgress {
  stage: string;
  current: number;
  total: number;
  message?: string;
  completed?: boolean;
  error?: string;
}

export interface SSEClientOptions {
  url: string;
  apiKey?: string;
  onProgress?: (progress: SSEProgress) => void;
  onEvent?: (event: SSEEvent) => void;
  onError?: (error: Error) => void;
  onComplete?: (data: unknown) => void;
}

/**
 * Connect to an SSE endpoint and stream progress events.
 * Uses native fetch with streaming for ESM compatibility.
 */
export async function connectSSE(options: SSEClientOptions): Promise<void> {
  const { url, onProgress, onEvent, onError, onComplete } = options;

  const apiKey = options.apiKey ?? getConfig()?.apiKey;
  if (!apiKey) {
    throw new Error('Not authenticated. Run `contento login` first.');
  }

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${apiKey}`,
      'Accept': 'text/event-stream',
      'Cache-Control': 'no-cache',
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`SSE connection failed (${response.status}): ${text}`);
  }

  if (!response.body) {
    throw new Error('SSE response has no body');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let currentEventType = 'message';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() ?? '';

      for (const line of lines) {
        const trimmed = line.trim();

        if (trimmed === '') {
          // End of event — reset event type
          currentEventType = 'message';
          continue;
        }

        if (trimmed.startsWith('event:')) {
          currentEventType = trimmed.slice(6).trim();
          continue;
        }

        if (trimmed.startsWith('data:')) {
          const raw = trimmed.slice(5).trim();
          if (!raw) continue;

          try {
            const parsed = JSON.parse(raw) as Record<string, unknown>;

            // Fire generic event callback
            if (onEvent) {
              onEvent({ type: currentEventType, data: parsed });
            }

            // Fire progress callback for progress-typed events
            if (currentEventType === 'progress' && onProgress) {
              onProgress(parsed as unknown as SSEProgress);
            }

            // Fire complete callback for done/complete events
            if ((currentEventType === 'complete' || currentEventType === 'done') && onComplete) {
              onComplete(parsed);
            }

            // Handle error events
            if (currentEventType === 'error' && onError) {
              const msg = (parsed.message as string) ?? (parsed.error as string) ?? 'Unknown SSE error';
              onError(new Error(msg));
            }
          } catch {
            // Data wasn't JSON — treat it as a plain string event
            if (onEvent) {
              onEvent({ type: currentEventType, data: raw });
            }
          }
        }
      }
    }
  } catch (err) {
    if (onError) {
      onError(err instanceof Error ? err : new Error(String(err)));
    } else {
      throw err;
    }
  }
}
