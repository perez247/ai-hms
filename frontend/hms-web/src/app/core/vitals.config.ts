/**
 * Single source of truth for vital-sign reference ranges.
 * Values outside these ranges are *accepted and saved* — the ranges are used
 * only to highlight abnormal readings in every vitals view.
 */
export interface VitalRange {
  readonly min: number;
  readonly max: number;
}

export const VITAL_RANGES = {
  temperatureC: { min: 36.1, max: 37.2 },
  systolic: { min: 90, max: 120 },
  diastolic: { min: 60, max: 80 },
  pulse: { min: 60, max: 100 },
  respiratoryRate: { min: 12, max: 20 },
  spo2: { min: 95, max: 100 },
  painScore: { min: 0, max: 3 },
} as const satisfies Record<string, VitalRange>;

export type VitalRangeKey = keyof typeof VITAL_RANGES;

export interface VitalReading {
  temperatureC: number | null;
  systolic: number | null;
  diastolic: number | null;
  pulse: number | null;
  respiratoryRate: number | null;
  spo2: number | null;
  painScore: number | null;
  [key: string]: unknown;
}

function outside(value: number | null | undefined, range: VitalRange): boolean {
  return value !== null && value !== undefined && (value < range.min || value > range.max);
}

/** True when any recorded component is outside its normal range. */
export function isVitalAbnormal(vital: VitalReading | null | undefined): boolean {
  if (!vital) return false;
  return (Object.keys(VITAL_RANGES) as VitalRangeKey[]).some((key) =>
    outside(vital[key] as number | null, VITAL_RANGES[key]),
  );
}

/** Returns the highlight CSS classes for a single component. */
export function vitalClass(value: number | null | undefined, key: VitalRangeKey): string {
  return outside(value, VITAL_RANGES[key]) ? 'font-bold text-red-600' : '';
}

/** Blood pressure is abnormal when either component is outside its range. */
export function bpClass(vital: VitalReading | null | undefined): string {
  if (!vital) return '';
  return outside(vital.systolic, VITAL_RANGES.systolic) || outside(vital.diastolic, VITAL_RANGES.diastolic)
    ? 'font-bold text-red-600'
    : '';
}

/** Human-readable reference range, useful for tooltips/labels. */
export function rangeLabel(key: VitalRangeKey): string {
  const range = VITAL_RANGES[key];
  return `${range.min}–${range.max}`;
}
