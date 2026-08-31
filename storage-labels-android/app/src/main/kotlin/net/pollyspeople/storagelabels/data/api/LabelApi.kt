package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.CreateLabelPrintJobRequest
import net.pollyspeople.storagelabels.data.dto.LabelPage
import net.pollyspeople.storagelabels.data.dto.LabelPrintJob
import net.pollyspeople.storagelabels.data.dto.UpdateLabelPrintJobRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface LabelApi {

    @GET("labels")
    suspend fun getJobs(): List<LabelPrintJob>

    @GET("labels/{jobId}")
    suspend fun getJob(@Path("jobId") jobId: String): LabelPrintJob

    @POST("labels")
    suspend fun createJob(@Body request: CreateLabelPrintJobRequest): LabelPrintJob

    @PUT("labels/{jobId}")
    suspend fun updateJob(
        @Path("jobId") jobId: String,
        @Body request: UpdateLabelPrintJobRequest,
    ): LabelPrintJob

    @DELETE("labels/{jobId}")
    suspend fun deleteJob(@Path("jobId") jobId: String): Response<Unit>

    /**
     * Allocates the next twelve codes and advances the job's counter server-side. Every call
     * consumes a sheet's worth of codes, so it must only ever run on a deliberate action.
     */
    @POST("labels/{jobId}/next-page")
    suspend fun getNextPage(@Path("jobId") jobId: String): LabelPage
}
