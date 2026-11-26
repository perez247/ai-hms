import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { debounce } from './debounce';

describe('debounce', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('calls the search once after the wait when the user types quickly', () => {
    const search = vi.fn();
    const onInput = debounce(search, 300);
    onInput('para');
    onInput('parac');
    onInput('paracetamol');
    expect(search).not.toHaveBeenCalled();
    vi.advanceTimersByTime(299);
    expect(search).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(search).toHaveBeenCalledOnce();
    expect(search).toHaveBeenCalledWith('paracetamol');
  });

  it('does not call the search when cancelled before the wait elapses', () => {
    const search = vi.fn();
    const onInput = debounce(search, 300);
    onInput('test');
    onInput.cancel();
    vi.advanceTimersByTime(500);
    expect(search).not.toHaveBeenCalled();
  });
});
