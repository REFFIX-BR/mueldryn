import { WarningBanner } from './WarningBanner';
import { Toolbar } from './Toolbar';
import { TimelineBar } from './TimelineBar';
import { CharacterLoadout } from '@/loadout/CharacterLoadout';
import { PropertiesPanel } from '@/editors/PropertiesPanel';
import { ViewportCanvas } from '@/viewport/CompareSplit';

export function App() {
  return (
    <div className="app-shell">
      <WarningBanner />
      <Toolbar />
      <CharacterLoadout />
      <div className="center-stage">
        <ViewportCanvas />
      </div>
      <PropertiesPanel />
      <TimelineBar />
    </div>
  );
}
