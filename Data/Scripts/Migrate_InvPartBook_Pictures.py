# -*- coding: utf-8 -*-
"""
Migrate Inv_PartBookSection.PictureContent (TotalSystem) → FileEntity + disk files (HavayarApp).
Requires: pyodbc
Does NOT write to TotalSystem.
"""
from __future__ import annotations

import os
import uuid
from datetime import datetime, timezone
from pathlib import Path

import pyodbc

HTS_CS = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=172.20.40.27;"
    "DATABASE=TotalSystem;"
    "UID=Developer;"
    "PWD=Dev#HyUser;"
    "TrustServerCertificate=yes;"
)
HAV_CS = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=172.20.40.42;"
    "DATABASE=HavayarApp;"
    "UID=sa;"
    "PWD=Sql123456$$;"
    "TrustServerCertificate=yes;"
)

# Prefer production uploads path; fall back to WebApp wwwroot
UPLOAD_CANDIDATES = [
    Path(r"D:\ApplicationData\uploads"),
    Path(r"D:\Projects\Havayar\HavayarApp\WebApp\wwwroot\Uploads"),
]


def pick_upload_root() -> Path:
    for p in UPLOAD_CANDIDATES:
        try:
            p.mkdir(parents=True, exist_ok=True)
            return p
        except OSError:
            continue
    raise SystemExit("No writable uploads path")


def guess_ext(name: str | None, blob: bytes) -> str:
    if name:
        lower = name.lower()
        for ext in (".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"):
            if lower.endswith(ext):
                return ext
    if blob[:3] == b"\xff\xd8\xff":
        return ".jpg"
    if blob[:8] == b"\x89PNG\r\n\x1a\n":
        return ".png"
    if blob[:2] == b"BM":
        return ".bmp"
    if blob[:6] in (b"GIF87a", b"GIF89a"):
        return ".gif"
    return ".jpg"


def content_type(ext: str) -> str:
    return {
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".png": "image/png",
        ".gif": "image/gif",
        ".bmp": "image/bmp",
        ".webp": "image/webp",
    }.get(ext.lower(), "application/octet-stream")


def main() -> None:
    upload_root = pick_upload_root()
    date_part = datetime.now(timezone.utc).strftime("%Y%m%d")
    target_dir = upload_root / date_part
    target_dir.mkdir(parents=True, exist_ok=True)

    hts = pyodbc.connect(HTS_CS)
    hav = pyodbc.connect(HAV_CS)
    hts.autocommit = False
    hav.autocommit = False

    src = hts.cursor()
    src.execute(
        """
        SELECT Id, PictureTitle, PictureContent
        FROM dbo.Inv_PartBookSection
        WHERE PictureContent IS NOT NULL AND DATALENGTH(PictureContent) > 0
        ORDER BY Id
        """
    )
    rows = src.fetchall()
    print(f"Source images: {len(rows)}")
    print(f"Upload root: {upload_root}")

    dst = hav.cursor()
    mapped = 0
    skipped = 0
    now = datetime.now()
    now_s = now.strftime("%Y-%m-%d %H:%M:%S")
    seed_user = "migrate-inv-partbook-pics"

    for hts_id, picture_title, blob in rows:
        if not blob:
            skipped += 1
            continue
        blob = bytes(blob)
        dst.execute(
            "SELECT Id, PictureId FROM Inv.PartBookSection WHERE HtsId = ?",
            hts_id,
        )
        sec = dst.fetchone()
        if not sec:
            skipped += 1
            continue
        section_id, old_picture_id = sec

        ext = guess_ext(picture_title, blob)
        unique = f"{uuid.uuid4().hex}{ext}"
        physical_rel = f"{date_part}/{unique}".replace("\\", "/")
        physical_abs = target_dir / unique
        physical_abs.write_bytes(blob)

        original = picture_title or f"partbook-{hts_id}{ext}"
        ct = content_type(ext)
        size = len(blob)

        dst.execute(
            """
            INSERT INTO dbo.FileEntity
                (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId,
                 CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
            OUTPUT INSERTED.Id
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1)
            """,
            physical_rel,
            original[:500] if original else unique,
            ct,
            size,
            "Entities.App.Inv.PartBookSection",
            "Picture",
            section_id,
            seed_user,
            now,
            now_s,
        )
        file_id = dst.fetchone()[0]

        dst.execute(
            "UPDATE Inv.PartBookSection SET PictureId = ?, PictureTitle = COALESCE(PictureTitle, ?) WHERE Id = ?",
            file_id,
            original[:255] if original else None,
            section_id,
        )
        mapped += 1
        if mapped % 10 == 0:
            hav.commit()
            print(f"  ... {mapped}")

    hav.commit()
    hts.close()
    hav.close()
    print(f"Done. Mapped={mapped}, Skipped={skipped}")


if __name__ == "__main__":
    main()
