"""Generate the learner handbook PDF from the canonical Markdown knowledge base."""
from __future__ import annotations

import re
from html import escape
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    BaseDocTemplate, Frame, KeepTogether, PageBreak, PageTemplate, Paragraph,
    Preformatted, Spacer, Table, TableStyle
)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output" / "pdf" / "Production_Support_Copilot_Hinglish_Handbook.pdf"

SOURCES = [
    ROOT / "PROJECT_LEARNING_JOURNAL_HINGLISH.md",
    ROOT / "docs" / "obsidian" / "03 Class and Service Catalog.md",
    ROOT / "docs" / "obsidian" / "04 Security Reliability and Evaluation.md",
    ROOT / "docs" / "obsidian" / "05 Runbook and Demo.md",
    ROOT / "docs" / "obsidian" / "06 System Design Interview.md",
    ROOT / "docs" / "obsidian" / "07 Glossary.md",
]

NAVY = colors.HexColor("#102A43")
BLUE = colors.HexColor("#1677B8")
TEAL = colors.HexColor("#087E8B")
LIGHT = colors.HexColor("#EEF6FA")
MUTED = colors.HexColor("#526777")
YELLOW = colors.HexColor("#FFF5CC")


class HandbookDoc(BaseDocTemplate):
    def __init__(self, filename: str):
        super().__init__(filename, pagesize=A4, leftMargin=19*mm, rightMargin=19*mm, topMargin=20*mm, bottomMargin=18*mm,
                         title="Production Support Copilot - Hinglish Handbook", author="Hisamuddin Siddique")
        frame = Frame(self.leftMargin, self.bottomMargin, self.width, self.height, id="normal")
        self.addPageTemplates(PageTemplate(id="main", frames=frame, onPage=self.header_footer))

    def header_footer(self, canvas, doc):
        canvas.saveState()
        canvas.setStrokeColor(colors.HexColor("#D7E3EA"))
        canvas.line(self.leftMargin, A4[1] - 13*mm, A4[0] - self.rightMargin, A4[1] - 13*mm)
        canvas.setFont("Helvetica", 8)
        canvas.setFillColor(MUTED)
        canvas.drawString(self.leftMargin, A4[1] - 10*mm, "Production Support Copilot | Applied AI + System Design")
        canvas.drawRightString(A4[0] - self.rightMargin, 10*mm, f"Page {doc.page}")
        canvas.restoreState()


def styles():
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle("TitleX", parent=base["Title"], fontName="Helvetica-Bold", fontSize=28, leading=33, textColor=NAVY, alignment=TA_CENTER, spaceAfter=12),
        "subtitle": ParagraphStyle("Subtitle", parent=base["Normal"], fontSize=12, leading=18, textColor=MUTED, alignment=TA_CENTER),
        "h1": ParagraphStyle("H1X", parent=base["Heading1"], fontName="Helvetica-Bold", fontSize=19, leading=23, textColor=NAVY, spaceBefore=14, spaceAfter=8),
        "h2": ParagraphStyle("H2X", parent=base["Heading2"], fontName="Helvetica-Bold", fontSize=14, leading=18, textColor=BLUE, spaceBefore=11, spaceAfter=6),
        "h3": ParagraphStyle("H3X", parent=base["Heading3"], fontName="Helvetica-Bold", fontSize=11.5, leading=15, textColor=TEAL, spaceBefore=8, spaceAfter=4),
        "body": ParagraphStyle("BodyX", parent=base["BodyText"], fontName="Helvetica", fontSize=9.3, leading=13.2, textColor=colors.HexColor("#243B53"), spaceAfter=5),
        "bullet": ParagraphStyle("BulletX", parent=base["BodyText"], fontSize=9.2, leading=12.8, leftIndent=12, firstLineIndent=-7, bulletIndent=2, spaceAfter=3),
        "code": ParagraphStyle("CodeX", parent=base["Code"], fontName="Courier", fontSize=7.2, leading=9.3, leftIndent=7, rightIndent=7, borderColor=colors.HexColor("#CBDDE7"), borderWidth=.5, borderPadding=7, backColor=colors.HexColor("#F5F8FA"), spaceBefore=4, spaceAfter=7),
        "quote": ParagraphStyle("QuoteX", parent=base["BodyText"], fontSize=9.2, leading=13, leftIndent=10, rightIndent=8, borderColor=TEAL, borderWidth=0, borderLeft=3, borderPadding=7, backColor=LIGHT, textColor=NAVY, spaceAfter=7),
        "small": ParagraphStyle("SmallX", parent=base["BodyText"], fontSize=7.5, leading=10, textColor=MUTED),
    }


def inline(text: str) -> str:
    text = escape(text)
    text = re.sub(r"`([^`]+)`", r"<font name='Courier'>\1</font>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"\[\[([^]|]+)\|?([^]]*)\]\]", lambda m: m.group(2) or m.group(1), text)
    return text


def markdown_story(path: Path, st: dict) -> list:
    lines = path.read_text(encoding="utf-8").splitlines()
    story, paragraph, code = [], [], []
    in_code = False

    def flush_paragraph():
        if paragraph:
            story.append(Paragraph(inline(" ".join(paragraph)), st["body"]))
            paragraph.clear()

    i = 0
    while i < len(lines):
        raw = lines[i].rstrip()
        if raw.startswith("```"):
            flush_paragraph()
            if in_code:
                story.append(Preformatted("\n".join(code), st["code"]))
                code.clear()
            in_code = not in_code
            i += 1
            continue
        if in_code:
            code.append(raw)
            i += 1
            continue
        if raw.startswith("|" ) and i + 1 < len(lines) and set(lines[i + 1].replace("|", "").replace("-", "").replace(":", "").replace(" ", "")) == set():
            flush_paragraph()
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                cells = [cell.strip() for cell in lines[i].strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", cell.replace(" ", "")) for cell in cells):
                    rows.append([Paragraph(inline(cell), st["small"]) for cell in cells])
                i += 1
            if rows:
                widths = [self_width for self_width in [165*mm / len(rows[0])] * len(rows[0])]
                table = Table(rows, colWidths=widths, repeatRows=1, hAlign="LEFT")
                table.setStyle(TableStyle([
                    ("BACKGROUND", (0, 0), (-1, 0), NAVY), ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
                    ("GRID", (0, 0), (-1, -1), .35, colors.HexColor("#B9CAD4")),
                    ("VALIGN", (0, 0), (-1, -1), "TOP"), ("BACKGROUND", (0, 1), (-1, -1), colors.white),
                    ("LEFTPADDING", (0, 0), (-1, -1), 4), ("RIGHTPADDING", (0, 0), (-1, -1), 4),
                    ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ]))
                story.extend([table, Spacer(1, 6)])
            continue
        if raw.startswith("# "):
            flush_paragraph(); story.extend([PageBreak(), Paragraph(inline(raw[2:]), st["h1"])]); i += 1; continue
        if raw.startswith("## "):
            flush_paragraph(); story.append(Paragraph(inline(raw[3:]), st["h1"])); i += 1; continue
        if raw.startswith("### "):
            flush_paragraph(); story.append(Paragraph(inline(raw[4:]), st["h2"])); i += 1; continue
        if raw.startswith("#### "):
            flush_paragraph(); story.append(Paragraph(inline(raw[5:]), st["h3"])); i += 1; continue
        if raw.startswith("> "):
            flush_paragraph(); story.append(Paragraph(inline(raw[2:]), st["quote"])); i += 1; continue
        if re.match(r"^[-*] ", raw):
            flush_paragraph(); story.append(Paragraph(inline(raw[2:]), st["bullet"], bulletText="-")); i += 1; continue
        number = re.match(r"^(\d+)\. (.+)", raw)
        if number:
            flush_paragraph(); story.append(Paragraph(inline(number.group(2)), st["bullet"], bulletText=number.group(1) + ".")); i += 1; continue
        if not raw:
            flush_paragraph()
        else:
            paragraph.append(raw)
        i += 1
    flush_paragraph()
    return story


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    st = styles()
    story = [Spacer(1, 35*mm), Paragraph("Production Support Copilot", st["title"]),
             Paragraph("Beginner-friendly Hinglish Handbook", st["title"]), Spacer(1, 8*mm),
             Paragraph("RAG, embeddings, vector search, LLM integration, Kafka, tools, approvals, security, evaluation and system design - explained through one runnable .NET 8 portfolio project.", st["subtitle"]),
             Spacer(1, 15*mm),
             Table([[Paragraph("LEARNING PATH", st["small"]), Paragraph("BUILD -> MEASURE -> SECURE -> EXPLAIN", st["small"])]], colWidths=[45*mm, 100*mm], style=TableStyle([("BACKGROUND", (0,0),(-1,-1), YELLOW), ("BOX", (0,0),(-1,-1), .5, colors.HexColor("#E0B84E")), ("PADDING", (0,0),(-1,-1), 8)])),
             Spacer(1, 35*mm), Paragraph("Generated from the repository's canonical Markdown knowledge base.", st["subtitle"]), PageBreak(),
             Paragraph("How to use this handbook", st["h1"]),
             Paragraph("First RAG chapters padho, then Swagger demo run karo, phir class catalog ke saath source files open karo. Interview chapter ko loud voice mein explain karo. Numbers marked as assumptions are capacity exercises, not measured production claims.", st["body"])]
    for source in SOURCES:
        story.extend(markdown_story(source, st))
    HandbookDoc(str(OUTPUT)).build(story)
    print(OUTPUT)


if __name__ == "__main__":
    build()
