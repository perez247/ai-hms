/** Tiny trailing-edge debounce used by search-as-you-type inputs. */
export function debounce<TArgs extends unknown[]>(fn: (...args: TArgs) => void, waitMs = 300) {
  let timer: ReturnType<typeof setTimeout> | undefined;
  const wrapped = (...args: TArgs): void => {
    if (timer !== undefined) clearTimeout(timer);
    timer = setTimeout(() => {
      timer = undefined;
      fn(...args);
    }, waitMs);
  };
  wrapped.cancel = (): void => {
    if (timer !== undefined) clearTimeout(timer);
    timer = undefined;
  };
  return wrapped;
}
