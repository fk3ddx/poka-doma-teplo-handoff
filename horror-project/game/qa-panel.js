// Explicit development-only controls, loaded solely with ?debug=1.
export function installQA(api){
 const panel=document.createElement('details');panel.id='qa-panel';panel.open=true;
 panel.style.cssText='position:fixed;right:8px;bottom:8px;z-index:50;background:#182020ed;padding:8px;color:#eee;font:11px monospace;max-width:440px;';
 panel.innerHTML='<summary>QA · служебные переходы</summary><div id="qa-controls" style="display:flex;flex-wrap:wrap;gap:4px"></div><pre id="qa-state" style="white-space:pre-wrap;max-height:120px;overflow:auto"></pre>';
 document.body.append(panel);const controls=panel.querySelector('#qa-controls');
 function button(label,fn){const b=document.createElement('button');b.textContent=label;b.style.cssText='min-width:0;padding:5px;font:11px monospace';b.onclick=fn;controls.append(b);}
 const points={coffee:[-4.65,-.30],note:[-4.1,-.37],phone:[-4.42,-2.1],window:[-3.25,-3.7],'front-door':[1.4,3.25],'kitchen-door':[1.25,-1.35],tap:[3.3,-3.6],handheld:[-3.1,-.36]};
 for(const [id,pos] of Object.entries(points))button('К '+id,()=>{api.close();api.pose(...pos);api.lookAt(...api.targets[id]);});
 button('Кухня / укрытие',()=>{api.close();api.pose(3.4,-.5,0,-.05);});
 button('+ 90 сек',()=>api.advance(90));button('+ 16 сек',()=>api.advance(16));
 button('Диван / обзор',()=>{api.close();api.pose(-1.2,-2.4,1.15,-.05);});
 button('Старт / обзор',()=>{api.close();api.pose(-4.72,.06,-.88,-.08);});
 button('Сохранить кадр',()=>api.capture());
 button('Дверь закрыть',()=>api.setDoor(true));button('Дверь открыть',()=>api.setDoor(false));
 setInterval(()=>{panel.querySelector('#qa-state').textContent=JSON.stringify(api.state,null,1);},500);
}
