"""Validate existing integrated-run evidence without relaunching or altering the game."""
from pathlib import Path
import json

def verify_integrated(out):
    out=Path(out)
    core=json.loads((out/'core-result.json').read_text(encoding='utf-8-sig'))
    checks=(out/'checks.txt').read_text(encoding='utf-8-sig').splitlines()
    assert core['passed'] and core['errors']==0 and core['graphicsDevice']!='Null',core
    assert not any(s.startswith(('FAIL ','ERROR ')) for s in checks),checks
    # Finale.Homely restores checkpoint 1; the post-transition lap is not the number completed.
    for lap in range(1,7):
        assert f'PASS lap {lap} entered' in checks,('missing real lap',lap)
    for event in ['chase entered','clock wound by holding E two seconds','ten strikes call the live story finale','six real laps and clock strikes entered the live finale']:
        assert 'PASS '+event in checks,event
    fin=json.loads((out/'finale/result.json').read_text(encoding='utf-8-sig'))
    fc=(out/'finale/checks.txt').read_text(encoding='utf-8-sig').splitlines()
    assert 'PASS continuing existing finale after real chase; no chapter reset' in fc
    assert fin['passed'] and fin['epilogue'] and fin['errors']==0 and fin['failures']==0,fin
    assert fin['beatOrder'][0]=='22:00' and fin['beatOrder'][-1]=='epilogue',fin
    assert len(list((out/'finale').glob('finale-*.png')))>=25
    mix=json.loads((out/'silent-mix.json').read_text(encoding='utf-8-sig'))
    assert mix['captureModeMutesOutput'] and mix['clearedOutputCallbacks']>0 and mix['droppedSamples']==0,mix
    return {'passed':True,'completedLaps':6,'postFinaleResetLap':core['lap'],'finaleBeats':fin['beats'],'epilogue':fin['epilogue'],'errors':core['errors']+fin['errors'],'scope':'Six laps/chase/live finale in one process; staged starting EnterLoop and finale camera, accelerated finale waits; not a human whole-game playthrough.'}

if __name__=='__main__':
    import argparse
    p=argparse.ArgumentParser();p.add_argument('directory');a=p.parse_args()
    result=verify_integrated(a.directory)
    target=Path(a.directory)/'validation.json'
    assert not target.exists(),'Do not overwrite existing validation evidence'
    target.write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))
