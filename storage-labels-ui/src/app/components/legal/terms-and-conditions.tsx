import React from 'react';
import { Box, Container, Paper, Typography, Link } from '@mui/material';

export const TermsConditions: React.FC = () => {
    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant="h3" component="h1" gutterBottom>
                    Terms of Service for Storage Labels
                </Typography>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Last updated: {new Date().toLocaleDateString()}
                </Typography>

                <Box sx={{ mt: 4 }}>
                    <Typography variant="h5" gutterBottom>
                        1. Acceptance of Terms
                    </Typography>
                    <Typography paragraph>
                        By accessing and using Storage Labels ("the Service"), you accept and agree to be bound by 
                        the terms and provisions of this agreement. If you do not agree to these terms, please do 
                        not use the Service.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        2. Description of Service
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is a web application that helps you organize and track your physical storage 
                        items. The Service allows you to:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Create and manage storage locations</li>
                        <li>Organize items into boxes</li>
                        <li>Upload and store images of your items</li>
                        <li>Search and track your stored items</li>
                        <li>Generate QR codes for easy item identification</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        3. Use of Service
                    </Typography>
                    <Typography paragraph>
                        You agree to use the Service in compliance with all applicable laws and regulations. 
                        You are prohibited from:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Using the Service for any illegal or unauthorized purpose</li>
                        <li>Attempting to gain unauthorized access to other users' data</li>
                        <li>Uploading malicious content, viruses, or harmful code</li>
                        <li>Violating the intellectual property rights of others</li>
                        <li>Interfering with or disrupting the Service or servers</li>
                        <li>Using automated systems to access the Service without permission</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        4. User Accounts
                    </Typography>
                    <Typography paragraph>
                        To use Storage Labels, you must authenticate using a Google account. You are responsible for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Maintaining the security of your Google account</li>
                        <li>All activities that occur under your account</li>
                        <li>Notifying us immediately of any unauthorized use of your account</li>
                        <li>Ensuring your account information is accurate and up-to-date</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        5. Data Ownership and Privacy
                    </Typography>
                    <Typography paragraph>
                        You retain all rights to the data you store in Storage Labels, including:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Box and item information you create</li>
                        <li>Images you upload</li>
                        <li>Location data you define</li>
                        <li>Any other content you add to the Service</li>
                    </Box>
                    <Typography paragraph>
                        Please review our <Link href="/legal/privacy">Privacy Policy</Link> to understand how 
                        we collect, use, and protect your data.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        6. Content Guidelines
                    </Typography>
                    <Typography paragraph>
                        When uploading images or creating content, you agree that:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>You own or have the right to use all content you upload</li>
                        <li>Your content does not violate any laws or third-party rights</li>
                        <li>Your content is not offensive, illegal, or inappropriate</li>
                        <li>You will not upload excessively large files that abuse storage limits</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        7. Service Availability
                    </Typography>
                    <Typography paragraph>
                        We strive to provide reliable service, but we do not guarantee that:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>The Service will be uninterrupted or error-free</li>
                        <li>Defects will be corrected immediately</li>
                        <li>The Service will be available at all times</li>
                        <li>The Service will meet your specific requirements</li>
                    </Box>
                    <Typography paragraph>
                        We reserve the right to modify, suspend, or discontinue any part of the Service at any time.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        8. Data Backup
                    </Typography>
                    <Typography paragraph>
                        While we implement reasonable data backup procedures, you are solely responsible for 
                        maintaining your own backup copies of your data. We are not liable for any data loss.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        9. Limitation of Liability
                    </Typography>
                    <Typography paragraph>
                        The Service is provided "as is" without warranty of any kind, either express or implied, 
                        including but not limited to warranties of merchantability, fitness for a particular purpose, 
                        or non-infringement.
                    </Typography>
                    <Typography paragraph>
                        We shall not be liable for any indirect, incidental, special, consequential, or punitive 
                        damages, including without limitation, loss of profits, data, use, or other intangible losses, 
                        resulting from your use of the Service.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        10. Indemnification
                    </Typography>
                    <Typography paragraph>
                        You agree to indemnify and hold harmless Storage Labels and its operators from any claims, 
                        damages, losses, liabilities, and expenses arising from your use of the Service or violation 
                        of these terms.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        11. Termination
                    </Typography>
                    <Typography paragraph>
                        We reserve the right to terminate or suspend your access to the Service at any time, 
                        with or without notice, for conduct that we believe:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Violates these Terms of Service</li>
                        <li>Is harmful to other users or the Service</li>
                        <li>Violates applicable laws or regulations</li>
                        <li>Exposes us to legal liability</li>
                    </Box>
                    <Typography paragraph>
                        You may terminate your account at any time by contacting us or ceasing to use the Service.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        12. Changes to Terms
                    </Typography>
                    <Typography paragraph>
                        We reserve the right to modify these terms at any time. We will notify users of any material 
                        changes by updating the "Last updated" date at the top of this page. Your continued use of 
                        the Service after such changes constitutes acceptance of the new terms.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        13. Governing Law
                    </Typography>
                    <Typography paragraph>
                        These terms shall be governed by and construed in accordance with applicable laws, without 
                        regard to conflict of law provisions.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        14. Contact Information
                    </Typography>
                    <Typography paragraph>
                        If you have any questions about these Terms of Service, please contact us at:{' '}
                        <Link href="mailto:support@pollyspeople.net">support@pollyspeople.net</Link>
                    </Typography>
                </Box>

                <Box sx={{ mt: 4, textAlign: 'center' }}>
                    <Link href="/" underline="hover">
                        Return to Storage Labels
                    </Link>
                </Box>
            </Paper>
        </Container>
    );
}