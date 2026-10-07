#!/usr/bin/env python3
"""Reviewed pixel landmarks from Hanley 1939 p85, not a present-day survey."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
# Source render is 1741 x 2400. Coordinates are outer wall faces, approximate.
left,right=282,1390
rear,front=1594,2187
central_left,central_right=552,1118
scale=36.576/(right-left)
result={
 'source':'https://ignca.gov.in/Asi_data/3497.pdf',
 'pdfPageOneBased':84,'printedPage':85,
 'render':'work/architecture/stockwell-source-084.png',
 'renderSizePixels':[1741,2400],
 'historicalOnly':True,'exactReplica':False,
 'calibration':{'labeledWidthFeet':120,'labeledWidthMetres':36.576,'pixelSpan':[left,right],'metresPerPixel':scale},
 'reviewedLandmarks':{'outerWallLeft':left,'outerWallRight':right,'rearWall':rear,'frontWall':front,'centralFacadeLeft':central_left,'centralFacadeRight':central_right},
 'derivedEstimatesMetres':{'overallDepth':round((front-rear)*scale,4),'centralFacadeWidth':round((central_right-central_left)*scale,4),'fiveBayPitch':round((central_right-central_left)*scale/5,4)},
 'limitations':['Pixel edges have approximately 6px reading uncertainty, before unknown scan distortion.','Depth uses the horizontal scale; independent vertical calibration is unavailable.','Central facade bounds are plan projections, not measured pilaster centre lines.','Room clear dimensions, wall thicknesses and current alterations are not resolved.','Do not interpret the outer 156/138-foot composition lines as building footprint dimensions.']}
path=ROOT/'Art/Architecture/Stockwell-shell-trace.json'
path.write_text(json.dumps(result,indent=2)+'\n')
assert 18<result['derivedEstimatesMetres']['centralFacadeWidth']<20
assert 18<result['derivedEstimatesMetres']['overallDepth']<21
print(json.dumps(result['derivedEstimatesMetres']))
