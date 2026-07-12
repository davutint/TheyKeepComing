from __future__ import annotations

import base64
import html
import mimetypes
from pathlib import Path

from docx import Document
from docx.document import Document as _Document
from docx.oxml.ns import qn
from docx.table import Table, _Cell
from docx.text.paragraph import Paragraph


ROOT = Path(r"C:\GithubProjeler\TheyKeepComing\IncremantalDots")
DOCX = ROOT / "Assets" / "Docs" / "DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.docx"
HTML = ROOT / "tmp" / "dead_walls_blueprint" / "blueprint_render.html"


def iter_block_items(parent):
    if isinstance(parent, _Document):
        parent_elm = parent.element.body
    elif isinstance(parent, _Cell):
        parent_elm = parent._tc
    else:
        raise ValueError("Unsupported parent")
    for child in parent_elm.iterchildren():
        if child.tag == qn("w:p"):
            yield Paragraph(child, parent)
        elif child.tag == qn("w:tbl"):
            yield Table(child, parent)


def paragraph_has_page_break(p: Paragraph) -> bool:
    return any(br.get(qn("w:type")) == "page" for br in p._p.xpath(".//w:br"))


def run_style(run) -> str:
    css = []
    if run.bold:
        css.append("font-weight:700")
    if run.italic:
        css.append("font-style:italic")
    if run.underline:
        css.append("text-decoration:underline")
    if run.font.size:
        css.append(f"font-size:{run.font.size.pt:.2f}pt")
    if run.font.color and run.font.color.rgb:
        css.append(f"color:#{run.font.color.rgb}")
    return ";".join(css)


def image_html(doc: Document, run) -> list[str]:
    items = []
    for blip in run._r.xpath(".//a:blip"):
        rid = blip.get(qn("r:embed"))
        if not rid or rid not in doc.part.rels:
            continue
        part = doc.part.rels[rid].target_part
        mime = part.content_type or mimetypes.guess_type(str(part.partname))[0] or "image/png"
        data = base64.b64encode(part.blob).decode("ascii")
        extents = run._r.xpath(".//wp:extent")
        width = 6.5
        if extents:
            try:
                width = min(7.15, int(extents[0].get("cx")) / 914400)
            except Exception:
                pass
        items.append(
            f'<img class="doc-image" style="width:{width:.3f}in" '
            f'src="data:{mime};base64,{data}" alt="Belge görseli">'
        )
    return items


def paragraph_html(doc: Document, p: Paragraph, in_cell: bool = False) -> str:
    style_name = p.style.name if p.style else "Normal"
    style_key = style_name.lower().replace(" ", "-")
    classes = ["paragraph", f"style-{style_key}"]
    if p._p.pPr is not None and p._p.pPr.numPr is not None:
        classes.append("bullet")
    align = p.alignment
    extra = []
    if align is not None:
        amap = {0: "left", 1: "center", 2: "right", 3: "justify"}
        if int(align) in amap:
            extra.append(f"text-align:{amap[int(align)]}")
    if p.paragraph_format.space_before:
        extra.append(f"margin-top:{p.paragraph_format.space_before.pt:.2f}pt")
    if p.paragraph_format.space_after:
        extra.append(f"margin-bottom:{p.paragraph_format.space_after.pt:.2f}pt")

    content = []
    for run in p.runs:
        content.extend(image_html(doc, run))
        if run.text:
            content.append(f'<span style="{run_style(run)}">{html.escape(run.text)}</span>')
    if not content:
        return '<div class="spacer"></div>' if not in_cell else ""
    tag = "p"
    if style_name == "Heading 1":
        tag = "h1"
    elif style_name == "Heading 2":
        tag = "h2"
    return f'<{tag} class="{" ".join(classes)}" style="{";".join(extra)}">{"".join(content)}</{tag}>'


def cell_fill(cell: _Cell) -> str | None:
    shd = cell._tc.xpath("./w:tcPr/w:shd")
    if shd:
        value = shd[0].get(qn("w:fill"))
        if value and value not in ("auto", "FFFFFF"):
            return value
    return None


def table_html(doc: Document, table: Table) -> str:
    rows = []
    for row in table.rows:
        cells = []
        seen = set()
        for cell in row.cells:
            identity = id(cell._tc)
            if identity in seen:
                continue
            seen.add(identity)
            fill = cell_fill(cell)
            style = f"background:#{fill};" if fill else ""
            paras = "".join(paragraph_html(doc, p, in_cell=True) for p in cell.paragraphs)
            cells.append(f'<td style="{style}">{paras}</td>')
        rows.append(f'<tr>{"".join(cells)}</tr>')
    return f'<table class="doc-table">{"".join(rows)}</table>'


def build_html() -> None:
    doc = Document(DOCX)
    pages: list[list[str]] = [[]]
    for block in iter_block_items(doc):
        if isinstance(block, Paragraph) and paragraph_has_page_break(block):
            if pages[-1]:
                pages.append([])
            continue
        if isinstance(block, Paragraph):
            pages[-1].append(paragraph_html(doc, block))
        else:
            pages[-1].append(table_html(doc, block))
    if not pages[-1]:
        pages.pop()

    rendered_pages = []
    total = len(pages)
    for index, blocks in enumerate(pages, 1):
        cover = " cover" if index == 1 else ""
        header = "" if index == 1 else '<div class="page-header"><span>DEAD WALLS</span><span>GAME DESIGN BLUEPRINT v1.0</span></div>'
        footer = "" if index == 1 else f'<div class="page-footer"><span>OWNER-APPROVED • 12 JULY 2026</span><span>{index:02d} / {total:02d}</span></div>'
        rendered_pages.append(
            f'<section class="page{cover}" data-page="{index}">{header}'
            f'<main>{"".join(blocks)}</main>{footer}</section>'
        )

    css = r"""
@page { size: 8.5in 11in; margin: 0; }
* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; background: #dfe5ec; color: #2F3B49; font-family: Arial, Helvetica, sans-serif; }
.page { position: relative; width: 8.5in; height: 11in; margin: 0 auto 12px; background: #FFFFFF; padding: 0.62in 0.68in 0.62in; overflow: hidden; break-after: page; page-break-after: always; }
.page:last-child { break-after: auto; page-break-after: auto; }
.page-header { position: absolute; left: 0.68in; right: 0.68in; top: 0.27in; display: flex; justify-content: space-between; border-bottom: 1px solid #D9E0E8; padding-bottom: 5px; color: #687789; font-size: 7.4pt; font-weight: 700; letter-spacing: .08em; }
.page-footer { position: absolute; left: 0.68in; right: 0.68in; bottom: 0.25in; display: flex; justify-content: space-between; border-top: 1px solid #D9E0E8; padding-top: 5px; color: #687789; font-size: 7pt; letter-spacing: .04em; }
main { height: 9.76in; overflow: hidden; }
.paragraph { margin: 0 0 6pt; font-size: 9.3pt; line-height: 1.26; orphans: 2; widows: 2; }
h1 { margin: 0 0 8pt; color: #0B1726; font-size: 22pt; line-height: 1.05; letter-spacing: -.02em; }
h2 { margin: 11pt 0 5pt; color: #18304E; font-size: 13pt; line-height: 1.1; }
.bullet { position: relative; padding-left: 14pt; }
.bullet:before { content: "•"; position: absolute; left: 1pt; top: 0; color: #B63A48; font-weight: 700; }
.spacer { height: 4pt; }
.doc-image { display: block; max-width: 100%; max-height: 5.75in; object-fit: contain; margin: 8pt auto; }
.doc-table { width: 100%; border-collapse: separate; border-spacing: 0; table-layout: fixed; margin: 5pt 0 8pt; font-size: 8.25pt; line-height: 1.18; border: 1px solid #D9E0E8; border-radius: 5px; overflow: hidden; }
.doc-table tr { break-inside: avoid; page-break-inside: avoid; }
.doc-table td { vertical-align: top; padding: 6px 7px; border-right: 1px solid #D9E0E8; border-bottom: 1px solid #D9E0E8; }
.doc-table tr:last-child td { border-bottom: 0; }
.doc-table td:last-child { border-right: 0; }
.doc-table .paragraph { font-size: inherit; margin-bottom: 2pt; }
.doc-table h1, .doc-table h2 { font-size: 9pt; margin: 0; }
.cover { padding-top: .45in; background: linear-gradient(155deg,#F6F8FB 0%,#FFFFFF 52%,#EAF2F8 100%); }
.cover main { height: 10.1in; }
.cover .paragraph:nth-child(1) { letter-spacing: .18em; color: #B63A48; font-weight: 700; font-size: 8pt; }
.cover .paragraph:nth-child(2) { color: #0B1726; font-size: 38pt; line-height: 1; font-weight: 700; margin-top: 7pt; }
.cover .paragraph:nth-child(3) { color: #18304E; font-size: 13pt; font-weight: 700; }
.cover .doc-image { width: 7.14in !important; height: 4.02in; object-fit: cover; border-radius: 12px; box-shadow: 0 7px 24px rgba(11,23,38,.22); margin: 14pt auto 12pt; }
.cover .paragraph:nth-child(5) { color: #0B1726; font-size: 16pt; line-height: 1.16; font-weight: 700; }
.cover .paragraph:nth-child(6) { color: #687789; font-size: 7.7pt; font-weight: 700; letter-spacing: .04em; border-top: 2px solid #B63A48; padding-top: 7pt; }
.cover .paragraph:nth-child(7) { color: #687789; font-size: 7.8pt; }
@media print { html, body { background: #FFFFFF; } .page { margin: 0; } }
"""
    output = f'<!doctype html><html lang="tr"><head><meta charset="utf-8"><style>{css}</style></head><body>{"".join(rendered_pages)}</body></html>'
    HTML.write_text(output, encoding="utf-8")
    print(f"pages={total}")
    print(HTML)


if __name__ == "__main__":
    build_html()
