package net.pollyspeople.storagelabels.core.labels

import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * Label geometry is the one thing in this app that a screenshot can't confirm — it has to
 * line up with a physical Avery 94107 sheet. These pin the measurements taken from the web
 * app's print stylesheet so they can't drift unnoticed.
 *
 * Reference (US Letter, 72pt/inch):
 *   top margin 0.5in, left margin 0.875in, 2in labels, 0.375in column gap, 0.5in row gap.
 */
class AveryLabelSheetTest {

    private val pointsPerInch = 72f

    @Test
    fun `page is us letter`() {
        assertEquals(8.5f, AveryLabelSheet.PAGE_WIDTH_PT / pointsPerInch, 0.001f)
        assertEquals(11f, AveryLabelSheet.PAGE_HEIGHT_PT / pointsPerInch, 0.001f)
    }

    @Test
    fun `labels are two inches square, twelve to a page`() {
        assertEquals(2f, AveryLabelSheet.LABEL_SIZE_PT / pointsPerInch, 0.001f)
        assertEquals(12, AveryLabelSheet.LABELS_PER_PAGE)
        assertEquals(3, AveryLabelSheet.COLUMNS)
        assertEquals(4, AveryLabelSheet.ROWS)
    }

    @Test
    fun `first label sits at the sheet margins`() {
        val (x, y) = AveryLabelSheet.labelOrigin(0)

        assertEquals(0.875f, x / pointsPerInch, 0.001f)
        assertEquals(0.5f, y / pointsPerInch, 0.001f)
    }

    @Test
    fun `columns step by the label width plus the column gap`() {
        val (firstX, _) = AveryLabelSheet.labelOrigin(0)
        val (secondX, _) = AveryLabelSheet.labelOrigin(1)
        val (thirdX, _) = AveryLabelSheet.labelOrigin(2)

        assertEquals(2.375f, (secondX - firstX) / pointsPerInch, 0.001f)
        assertEquals(2.375f, (thirdX - secondX) / pointsPerInch, 0.001f)
    }

    @Test
    fun `rows step by the label height plus the row gap`() {
        val (_, firstY) = AveryLabelSheet.labelOrigin(0)
        val (_, secondRowY) = AveryLabelSheet.labelOrigin(3)

        assertEquals(2.5f, (secondRowY - firstY) / pointsPerInch, 0.001f)
    }

    @Test
    fun `the grid is row-major, so label four starts the second row`() {
        val (x, _) = AveryLabelSheet.labelOrigin(3)
        val (firstX, _) = AveryLabelSheet.labelOrigin(0)

        assertEquals(firstX, x, 0.001f)
    }

    @Test
    fun `the last label stays on the page`() {
        val (x, y) = AveryLabelSheet.labelOrigin(AveryLabelSheet.LABELS_PER_PAGE - 1)

        val right = x + AveryLabelSheet.LABEL_SIZE_PT
        val bottom = y + AveryLabelSheet.LABEL_SIZE_PT

        assert(right <= AveryLabelSheet.PAGE_WIDTH_PT) { "Label runs off the right edge: $right" }
        assert(bottom <= AveryLabelSheet.PAGE_HEIGHT_PT) { "Label runs off the bottom: $bottom" }
    }
}
