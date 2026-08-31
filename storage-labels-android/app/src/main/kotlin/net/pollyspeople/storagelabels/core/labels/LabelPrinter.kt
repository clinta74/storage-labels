package net.pollyspeople.storagelabels.core.labels

import android.content.Context
import android.graphics.pdf.PdfDocument
import android.os.Bundle
import android.os.CancellationSignal
import android.os.ParcelFileDescriptor
import android.print.PageRange
import android.print.PrintAttributes
import android.print.PrintDocumentAdapter
import android.print.PrintDocumentInfo
import android.print.PrintManager
import androidx.core.content.ContextCompat
import net.pollyspeople.storagelabels.data.dto.LabelCodeItem
import java.io.FileOutputStream

/**
 * Hands a sheet of labels to Android's print system.
 *
 * The page is laid out at fixed Letter geometry rather than adapting to the printer's
 * reported margins: Avery stock has the labels in fixed positions, so scaling the page to
 * fit a printer's imageable area would put the ink between the labels.
 */
object LabelPrinter {

    fun print(
        context: Context,
        jobName: String,
        labels: List<LabelCodeItem>,
        codeColorPattern: String,
    ) {
        val printManager = ContextCompat.getSystemService(context, PrintManager::class.java)
            ?: return

        val attributes = PrintAttributes.Builder()
            .setMediaSize(PrintAttributes.MediaSize.NA_LETTER)
            // The sheet's own margins are part of the label geometry.
            .setMinMargins(PrintAttributes.Margins.NO_MARGINS)
            .setColorMode(PrintAttributes.COLOR_MODE_COLOR)
            .build()

        printManager.print(jobName, LabelSheetAdapter(jobName, labels, codeColorPattern), attributes)
    }
}

private class LabelSheetAdapter(
    private val jobName: String,
    private val labels: List<LabelCodeItem>,
    private val codeColorPattern: String,
) : PrintDocumentAdapter() {

    override fun onLayout(
        oldAttributes: PrintAttributes?,
        newAttributes: PrintAttributes?,
        cancellationSignal: CancellationSignal?,
        callback: LayoutResultCallback,
        extras: Bundle?,
    ) {
        if (cancellationSignal?.isCanceled == true) {
            callback.onLayoutCancelled()
            return
        }

        val info = PrintDocumentInfo.Builder("$jobName.pdf")
            .setContentType(PrintDocumentInfo.CONTENT_TYPE_DOCUMENT)
            .setPageCount(1)
            .build()

        callback.onLayoutFinished(info, true)
    }

    override fun onWrite(
        pages: Array<out PageRange>?,
        destination: ParcelFileDescriptor,
        cancellationSignal: CancellationSignal?,
        callback: WriteResultCallback,
    ) {
        val document = PdfDocument()
        try {
            val pageInfo = PdfDocument.PageInfo.Builder(
                AveryLabelSheet.PAGE_WIDTH_PT,
                AveryLabelSheet.PAGE_HEIGHT_PT,
                1,
            ).create()

            val page = document.startPage(pageInfo)
            AveryLabelSheet.drawSheet(page.canvas, labels, codeColorPattern)
            document.finishPage(page)

            if (cancellationSignal?.isCanceled == true) {
                callback.onWriteCancelled()
                return
            }

            FileOutputStream(destination.fileDescriptor).use(document::writeTo)
            callback.onWriteFinished(arrayOf(PageRange.ALL_PAGES))
        } catch (error: Exception) {
            callback.onWriteFailed(error.message)
        } finally {
            document.close()
        }
    }
}
