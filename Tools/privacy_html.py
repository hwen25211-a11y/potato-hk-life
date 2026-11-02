#!/usr/bin/env python3
"""Writes store/privacy.html from the in-game policy (Assets/Scripts/PotatoPrivacy.cs) and TapConfig, so the TapTap
listing links to exactly the text players see. Host the file anywhere public and paste its URL into the developer console."""
import html,os,re
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src=open(os.path.join(ROOT,'Assets','Scripts','PotatoPrivacy.cs'),encoding='utf8').read()
cfg=open(os.path.join(ROOT,'Assets','Scripts','PotatoTapGate.cs'),encoding='utf8').read()
const=lambda name,text:re.search(r'const string '+name+r'="(.*?)";',text).group(1)
body=re.search(r'const string Body=@"(.*?)";',src,re.S).group(1).replace('""','"')
dev,mail=const('Developer',cfg),const('Email',cfg)
body=body.replace('{开发者}',dev or '（开发者名称待填写）').replace('{邮箱}',mail or '（联系邮箱待填写）').replace('{日期}',const('Updated',src))
lines=body.strip().split('\n');title=lines[0]
out=[]
for line in lines[1:]:
    t=html.escape(line)
    t=re.sub(r'(https://\S+)',r'<a href="\1">\1</a>',t)
    if not line.strip():continue
    out.append('<h2>%s</h2>'%t if re.match(r'^[一二三四五六七八九十]、',line) else '<p>%s</p>'%t)
page='''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>%s</title><style>body{margin:0;background:#FFF8EC;color:#34556A;font:16px/1.75 -apple-system,"PingFang SC","Microsoft YaHei",sans-serif}
main{max-width:760px;margin:0 auto;padding:32px 16px 64px}h1{font-size:26px}h2{font-size:19px;margin-top:28px}a{color:#3A7CA5;word-break:break-all}</style></head>
<body><main><h1>%s</h1>
%s
</main></body></html>
'''%(html.escape(title),html.escape(title),'\n'.join(out))
os.makedirs(os.path.join(ROOT,'store'),exist_ok=True)
open(os.path.join(ROOT,'store','privacy.html'),'w',encoding='utf8').write(page)
print('store/privacy.html',('' if dev and mail else '（开发者名称或邮箱还没填）'))
