import { PLACEHOLDER_BONES } from '@/mu/bones';

export function AttachmentPicker({
  bone,
  boneIndex,
  onChange,
}: {
  bone: string;
  boneIndex: number;
  onChange: (bone: string, boneIndex: number) => void;
}) {
  return (
    <div className="grid-2" style={{ marginBottom: 8 }}>
      <label className="field">
        Attach bone
        <select
          value={bone}
          onChange={(e) => {
            const b = PLACEHOLDER_BONES.find((x) => x.name === e.target.value);
            if (b) onChange(b.name, b.index);
          }}
        >
          {PLACEHOLDER_BONES.map((b) => (
            <option key={b.name} value={b.name}>
              {b.name} ({b.index})
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        Bone index
        <input
          type="number"
          value={boneIndex}
          onChange={(e) => {
            const idx = Number(e.target.value);
            const b = PLACEHOLDER_BONES.find((x) => x.index === idx);
            onChange(b?.name ?? String(idx), idx);
          }}
        />
      </label>
    </div>
  );
}
