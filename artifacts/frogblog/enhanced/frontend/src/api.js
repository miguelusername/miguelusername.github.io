/**
 * Single place the frontend talks to the backend from.
 * The base URL comes from VITE_API_URL (see .env.development) so nothing is hardcoded.
 */
export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:3000';

/**
 * fetch() with the defaults every backend call needs: the session cookie,
 * a JSON body when one is given, and a parsed JSON error message on failure.
 */
export async function apiFetch(path, { method = 'GET', body, ...options } = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    method,
    credentials: 'include',
    headers: {
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...(options.headers ?? {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
    ...options,
  });

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    let details;
    try {
      const data = await response.json();
      if (data?.error) message = data.error;
      details = data?.details;
    } catch {
      // Non-JSON error body; keep the generic message.
    }
    const error = new Error(message);
    error.status = response.status;
    error.details = details;
    throw error;
  }

  return response.status === 204 ? null : response.json();
}
