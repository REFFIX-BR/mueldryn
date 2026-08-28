/** MuMain MapFileDecrypt — used by BMD v12 payloads. */
const XOR_KEY = new Uint8Array([
  0xd1, 0x73, 0x52, 0xf6, 0xd2, 0x9a, 0xcb, 0x27, 0x3e, 0xaf, 0x59, 0x31, 0x37, 0xb3, 0xe7, 0xa2,
]);

export function mapFileDecrypt(source: Uint8Array): Uint8Array {
  const dst = new Uint8Array(source.length);
  let mapKey = 0x5e;
  for (let i = 0; i < source.length; i++) {
    dst[i] = ((source[i] ^ XOR_KEY[i % 16]) - mapKey) & 0xff;
    mapKey = (source[i] + 0x3d) & 0xff;
  }
  return dst;
}
