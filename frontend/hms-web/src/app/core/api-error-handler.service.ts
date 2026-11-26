import { inject, Injectable } from '@angular/core';
import { ToastService } from './toast.service';

/** Shape of the RFC 7807 problem responses returned by the API. */
export interface ApiProblem {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface ApiErrorLike {
  error?: ApiProblem | string | null;
  status?: number;
  message?: string;
}

const GENERIC_MESSAGE = 'Something went wrong. Please try again.';

/**
 * Single place that turns any API failure into one user-facing message.
 * Field-level validation messages win over the generic `detail`; the traceId is
 * logged for support but never shown in the UI.
 */
@Injectable({ providedIn: 'root' })
export class ApiErrorHandlerService {
  private readonly toast = inject(ToastService);

  /** Returns the message that was displayed (useful for tests). */
  show(error: unknown): string {
    const problem = this.problemOf(error);
    const message = this.messageOf(problem, error);
    if (problem?.traceId) console.error(`[api-error] traceId=${problem.traceId}`, problem);
    else console.error('[api-error]', error);
    this.toast.error(message);
    return message;
  }

  /** Extracts the user-facing message without showing a toast. */
  messageOf(problem: ApiProblem | null, error?: unknown): string {
    const errors = problem?.errors;
    if (errors && Object.keys(errors).length > 0) {
      const firstKey = Object.keys(errors)[0];
      const messages = errors[firstKey];
      if (Array.isArray(messages) && messages.length > 0 && messages[0]) return messages[0];
    }
    const detail = problem?.detail;
    if (typeof detail === 'string' && detail.trim()) return detail;
    if (problem && typeof problem === 'object' && typeof problem.title === 'string'
        && problem.title.trim() && problem.title !== 'Problem') return problem.title;
    if (error instanceof Error && error.message.trim()) return error.message;
    return GENERIC_MESSAGE;
  }

  problemOf(error: unknown): ApiProblem | null {
    if (!error || typeof error !== 'object') return null;
    const body = (error as ApiErrorLike).error;
    if (body && typeof body === 'object') return body as ApiProblem;
    return null;
  }
}
