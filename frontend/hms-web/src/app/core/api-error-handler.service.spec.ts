import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { ApiErrorHandlerService } from './api-error-handler.service';
import { ToastService } from './toast.service';

describe('ApiErrorHandlerService', () => {
  const toast = { error: vi.fn(), success: vi.fn(), info: vi.fn() };

  function setup() {
    toast.error.mockClear();
    TestBed.configureTestingModule({ providers: [{ provide: ToastService, useValue: toast }] });
    return TestBed.inject(ApiErrorHandlerService);
  }

  const httpError = (body: unknown) => ({ status: 400, error: body });

  it('shows the first message of the first field and ignores the generic detail', () => {
    const handler = setup();
    const message = handler.show(httpError({
      title: 'Validation failed', status: 400,
      detail: 'One or more request fields are invalid.',
      errors: { 'Request.Pulse': ["'Request Pulse' must be between 20 and 300. You entered 12."] },
      traceId: 'trace-1',
    }));

    expect(message).toBe("'Request Pulse' must be between 20 and 300. You entered 12.");
    expect(toast.error).toHaveBeenCalledWith(message);
    expect(message).not.toContain('One or more request fields are invalid.');
  });

  it('uses the first message of the first field when several fields fail', () => {
    const handler = setup();
    const message = handler.show(httpError({
      errors: {
        'Request.Temperature': ['Temperature is required.', 'Temperature must be numeric.'],
        'Request.Pulse': ['Pulse is required.'],
      },
      detail: 'One or more request fields are invalid.',
    }));

    expect(message).toBe('Temperature is required.');
    expect(toast.error).toHaveBeenCalledOnce();
  });

  it('falls back to detail when there is no errors object', () => {
    const handler = setup();
    const message = handler.show(httpError({ title: 'Conflict', status: 409, detail: 'Doctor not found.' }));
    expect(message).toBe('Doctor not found.');
  });

  it('falls back to a generic message when there is no usable payload', () => {
    const handler = setup();
    expect(handler.show({ status: 500, error: null })).toBe('Something went wrong. Please try again.');
    expect(handler.show(null)).toBe('Something went wrong. Please try again.');
  });

  it('logs the traceId for support without showing it to the user', () => {
    const handler = setup();
    const spy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const message = handler.show(httpError({
      detail: 'Bad request', traceId: '0HNQ9GOSHU5LQ:0000004E',
      errors: { Field: ['Field is invalid.'] },
    }));
    expect(spy).toHaveBeenCalledWith(expect.stringContaining('0HNQ9GOSHU5LQ:0000004E'), expect.anything());
    expect(message).not.toContain('0HNQ9GOSHU5LQ');
    spy.mockRestore();
  });
});
