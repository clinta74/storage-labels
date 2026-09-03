package net.pollyspeople.storagelabels.core.labels

import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel
import net.pollyspeople.storagelabels.core.code.CodeColorPattern
import net.pollyspeople.storagelabels.core.code.CodeColorPattern.SegmentColor
import net.pollyspeople.storagelabels.data.dto.LabelCodeItem
import java.io.OutputStream

/**
 * Renders a sheet of Avery 94107 labels as a PDF page.
 *
 * The geometry is the whole point and is copied from the web app's print stylesheet, in
 * points (72 per inch) on US Letter:
 *
 *   sheet        8.5in x 11in       612 x 792
 *   label        2in x 2in          144 x 144
 *   top margin   0.5in              36
 *   left margin  0.875in            63
 *   column gap   0.375in            27
 *   row gap      0.5in              36
 *   grid         3 columns x 4 rows = 12 labels
 *
 * If these numbers drift the labels stop lining up with the physical sheet, so they are
 * pinned by AveryLabelSheetTest.
 */
object AveryLabelSheet {

    const val PAGE_WIDTH_PT = 612
    const val PAGE_HEIGHT_PT = 792
    const val LABEL_SIZE_PT = 144f
    const val MARGIN_TOP_PT = 36f
    const val MARGIN_LEFT_PT = 63f
    const val COLUMN_GAP_PT = 27f
    const val ROW_GAP_PT = 36f
    const val COLUMNS = 3
    const val ROWS = 4
    const val LABELS_PER_PAGE = COLUMNS * ROWS

    private const val QR_SIZE_PT = 104f
    private const val CODE_TEXT_SIZE_PT = 12f
    private const val QR_TEXT_GAP_PT = 6f

    /** Top-left corner of the label at [index] (0-based, row-major), in points. */
    fun labelOrigin(index: Int): Pair<Float, Float> {
        val column = index % COLUMNS
        val row = index / COLUMNS
        val x = MARGIN_LEFT_PT + column * (LABEL_SIZE_PT + COLUMN_GAP_PT)
        val y = MARGIN_TOP_PT + row * (LABEL_SIZE_PT + ROW_GAP_PT)
        return x to y
    }

    /**
     * Writes a one-page PDF of up to [LABELS_PER_PAGE] labels. Caller owns [output].
     */
    fun writePdf(
        labels: List<LabelCodeItem>,
        codeColorPattern: String,
        output: OutputStream,
    ) {
        val document = PdfDocument()
        try {
            val pageInfo = PdfDocument.PageInfo.Builder(PAGE_WIDTH_PT, PAGE_HEIGHT_PT, 1).create()
            val page = document.startPage(pageInfo)
            drawSheet(page.canvas, labels, codeColorPattern)
            document.finishPage(page)
            document.writeTo(output)
        } finally {
            document.close()
        }
    }

    fun drawSheet(canvas: Canvas, labels: List<LabelCodeItem>, codeColorPattern: String) {
        labels.take(LABELS_PER_PAGE).forEachIndexed { index, label ->
            val (x, y) = labelOrigin(index)
            drawLabel(canvas, x, y, label.code, codeColorPattern)
        }
    }

    private fun drawLabel(
        canvas: Canvas,
        left: Float,
        top: Float,
        code: String,
        codeColorPattern: String,
    ) {
        val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            typeface = Typeface.create(Typeface.MONOSPACE, Typeface.BOLD)
            textSize = CODE_TEXT_SIZE_PT
        }

        val segments = CodeColorPattern.parse(codeColorPattern, code)
        val totalTextWidth = segments.sumOf { textPaint.measureText(it.text).toDouble() }.toFloat()

        // The QR block and the code line are centred as a unit inside the 2in square.
        val contentHeight = QR_SIZE_PT + QR_TEXT_GAP_PT + textPaint.textSize
        val qrTop = top + (LABEL_SIZE_PT - contentHeight) / 2f
        val qrLeft = left + (LABEL_SIZE_PT - QR_SIZE_PT) / 2f

        drawQrCode(canvas, code, qrLeft, qrTop, QR_SIZE_PT)

        var textX = left + (LABEL_SIZE_PT - totalTextWidth) / 2f
        val textY = qrTop + QR_SIZE_PT + QR_TEXT_GAP_PT + textPaint.textSize
        segments.forEach { segment ->
            textPaint.color = segment.color.toPrintColor()
            canvas.drawText(segment.text, textX, textY, textPaint)
            textX += textPaint.measureText(segment.text)
        }
    }

    private fun drawQrCode(canvas: Canvas, content: String, left: Float, top: Float, size: Float) {
        val writer = QRCodeWriter()
        val hints = mapOf(
            // M matches the web app's qrcode.react level; margin 0 because the label
            // already has quiet space around it.
            EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.M,
            EncodeHintType.MARGIN to 0,
        )
        val matrix = runCatching {
            writer.encode(content, BarcodeFormat.QR_CODE, MODULE_RESOLUTION, MODULE_RESOLUTION, hints)
        }.getOrNull() ?: return

        val modules = matrix.width
        val moduleSize = size / modules
        val paint = Paint().apply { color = Color.BLACK }

        for (y in 0 until modules) {
            for (x in 0 until modules) {
                if (matrix.get(x, y)) {
                    canvas.drawRect(
                        left + x * moduleSize,
                        top + y * moduleSize,
                        left + (x + 1) * moduleSize,
                        top + (y + 1) * moduleSize,
                        paint,
                    )
                }
            }
        }
    }

    /**
     * Print colours, not screen colours: these stay legible on white paper and are close to
     * the web theme's palette so a label looks the same whichever client printed it.
     */
    private fun SegmentColor.toPrintColor(): Int = when (this) {
        SegmentColor.Primary -> Color.rgb(0x0E, 0x63, 0x55)
        SegmentColor.Secondary -> Color.rgb(0x1F, 0x4F, 0xA8)
        SegmentColor.Error -> Color.rgb(0x8C, 0x1D, 0x18)
        SegmentColor.Warning -> Color.rgb(0x8A, 0x5A, 0x00)
        SegmentColor.Info -> Color.rgb(0x1F, 0x4F, 0xA8)
        SegmentColor.Success -> Color.rgb(0x2E, 0x7D, 0x32)
        SegmentColor.Default -> Color.BLACK
    }

    /** Requested QR resolution; ZXing rounds to whole modules. */
    private const val MODULE_RESOLUTION = 512
}
