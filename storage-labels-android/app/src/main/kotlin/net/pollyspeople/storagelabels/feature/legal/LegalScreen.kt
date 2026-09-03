package net.pollyspeople.storagelabels.feature.legal

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun LegalScreen(document: LegalDocument, modifier: Modifier = Modifier) {
    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            Text(document.title, style = MaterialTheme.typography.headlineSmall)
        }

        document.sections.forEach { section ->
            item {
                Text(
                    section.heading,
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(top = 8.dp),
                )
            }
            items(section.blocks.size) { index ->
                when (val block = section.blocks[index]) {
                    is LegalBlock.Paragraph -> Text(
                        block.text,
                        style = MaterialTheme.typography.bodyMedium,
                    )

                    is LegalBlock.Bullet -> Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        Text("•", style = MaterialTheme.typography.bodyMedium)
                        Text(block.text, style = MaterialTheme.typography.bodyMedium)
                    }
                }
            }
        }

        item {
            Column(Modifier.padding(top = 16.dp)) {
                Text(
                    "This is the same text the web app shows. For anything specific to this " +
                        "installation, ask whoever runs the server.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}

@Composable
fun PrivacyScreen(modifier: Modifier = Modifier) = LegalScreen(PrivacyNotice, modifier)

@Composable
fun TermsScreen(modifier: Modifier = Modifier) = LegalScreen(TermsAndConditions, modifier)
