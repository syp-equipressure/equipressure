import { DeviceFrameParser } from './device-frame-parser';

describe('DeviceFrameParser', () => {
  const fullFrame = JSON.stringify(Array.from({ length: 400 }, (_, index) => index));

  it('joins a frame that arrives in parts and converts mbar to N/cm²', () => {
    const parser = new DeviceFrameParser();

    expect(parser.add(fullFrame.slice(0, 100))).toEqual([]);
    const frames = parser.add(`${fullFrame.slice(100)}\n`);

    expect(frames.length).toBe(1);
    expect(frames[0].length).toBe(20);
    expect(frames[0][19][19]).toBeCloseTo(3.99);
  });

  it('returns several frames of one message', () => {
    expect(new DeviceFrameParser().add(`${fullFrame}\r\n${fullFrame}\n`).length).toBe(2);
  });

  it('stretches short values to the heatmap grid', () => {
    const frames = new DeviceFrameParser().add('[12.5, 13.1, 14.8]\n12.5\n');

    expect(frames.length).toBe(2);
    expect(frames.every(frame => frame.length === 20 && frame[0].length === 20)).toBeTrue();
  });

  it('skips invalid lines', () => {
    expect(new DeviceFrameParser().add('kaputt\n[1, 2\n\n')).toEqual([]);
  });

  it('forgets a started line after reset', () => {
    const parser = new DeviceFrameParser();
    parser.add('[1, 2');

    parser.reset();

    expect(parser.add('5\n').length).toBe(1);
  });
});
