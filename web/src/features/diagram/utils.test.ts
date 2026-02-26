import { describe, it, expect } from 'vitest';
import { trafficColor, healthColor } from './utils';

describe('trafficColor', () => {
  it('returns green hex for traffic >= 0.8', () => {
    expect(trafficColor(0.9)).toBe('#2e8f5e');
  });

  it('returns yellow hex for traffic >= 0.5 and < 0.8', () => {
    expect(trafficColor(0.6)).toBe('#9d7c35');
  });

  it('returns red hex for traffic < 0.5', () => {
    expect(trafficColor(0.3)).toBe('#9e3a3a');
  });

  it('returns green hex at boundary 0.8', () => {
    expect(trafficColor(0.8)).toBe('#2e8f5e');
  });

  it('returns yellow hex at boundary 0.5', () => {
    expect(trafficColor(0.5)).toBe('#9d7c35');
  });
});

describe('healthColor', () => {
  it('returns green hex for green', () => {
    expect(healthColor('green')).toBe('#2e8f5e');
  });

  it('returns yellow hex for yellow', () => {
    expect(healthColor('yellow')).toBe('#9d7c35');
  });

  it('returns red hex for red', () => {
    expect(healthColor('red')).toBe('#9e3a3a');
  });
});
