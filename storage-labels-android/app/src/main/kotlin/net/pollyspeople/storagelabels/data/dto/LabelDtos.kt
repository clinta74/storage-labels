package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
enum class LabelFormat {
    /** 2in x 2in, 12 per US Letter sheet, 3 columns x 4 rows. */
    @SerialName("Avery94107")
    Avery94107,
}

@Serializable
enum class LabelIncrementAlgorithm {
    @SerialName("NumericOnly")
    NumericOnly,

    @SerialName("Base36Suffix")
    Base36Suffix,
}

@Serializable
data class LabelPrintJob(
    val id: String,
    val name: String,
    val labelFormat: LabelFormat = LabelFormat.Avery94107,
    val incrementAlgorithm: LabelIncrementAlgorithm = LabelIncrementAlgorithm.Base36Suffix,
    val algorithmPrefix: String? = null,
    val algorithmSuffixLength: Int = 4,
    val lastGeneratedIndex: Long = 0,
    val totalLabelsGenerated: Long = 0,
    val codeColorPattern: String = "",
    val createdAt: String? = null,
)

@Serializable
data class CreateLabelPrintJobRequest(
    val name: String,
    val labelFormat: LabelFormat,
    val incrementAlgorithm: LabelIncrementAlgorithm,
    val algorithmPrefix: String? = null,
    val algorithmSuffixLength: Int,
    val startIndex: Long,
    val codeColorPattern: String,
)

@Serializable
data class UpdateLabelPrintJobRequest(
    val name: String,
    val labelFormat: LabelFormat,
    val incrementAlgorithm: LabelIncrementAlgorithm,
    val algorithmPrefix: String? = null,
    val algorithmSuffixLength: Int,
    val codeColorPattern: String,
)

@Serializable
data class LabelCodeItem(
    val code: String,
    val labelNumber: Long,
)

@Serializable
data class LabelPage(
    val jobId: String,
    val labelFormat: LabelFormat = LabelFormat.Avery94107,
    val codeColorPattern: String = "",
    val labels: List<LabelCodeItem> = emptyList(),
)
