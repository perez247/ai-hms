import { VITAL_RANGES, bpClass, isVitalAbnormal, vitalClass } from './vitals.config';

describe('vital sign reference ranges', () => {
  const base = {
    temperatureC: 36.8, systolic: 118, diastolic: 78, pulse: 72,
    respiratoryRate: 16, spo2: 98, painScore: 1,
  };

  it('does not flag readings inside the shared normal ranges', () => {
    expect(isVitalAbnormal(base)).toBe(false);
    expect(vitalClass(base.temperatureC, 'temperatureC')).toBe('');
    expect(bpClass(base)).toBe('');
  });

  it('flags and highlights abnormal values while leaving normal ones alone', () => {
    const abnormal = { ...base, temperatureC: 39.4, spo2: 91, painScore: 8 };
    expect(isVitalAbnormal(abnormal)).toBe(true);
    expect(vitalClass(abnormal.temperatureC, 'temperatureC')).toContain('text-red-600');
    expect(vitalClass(abnormal.spo2, 'spo2')).toContain('text-red-600');
    expect(vitalClass(abnormal.painScore, 'painScore')).toContain('text-red-600');
    expect(vitalClass(abnormal.pulse, 'pulse')).toBe('');
  });

  it('treats either abnormal blood pressure component as abnormal', () => {
    expect(bpClass({ ...base, systolic: 145 })).toContain('text-red-600');
    expect(bpClass({ ...base, diastolic: 55 })).toContain('text-red-600');
  });

  it('exposes a single reusable range definition', () => {
    expect(VITAL_RANGES.temperatureC).toEqual({ min: 36.1, max: 37.2 });
    expect(VITAL_RANGES.spo2.min).toBe(95);
  });
});
