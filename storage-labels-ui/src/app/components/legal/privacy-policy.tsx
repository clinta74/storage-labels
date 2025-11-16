import React from "react"
import { Box, Container, Paper, Typography, Link } from '@mui/material';

export const PrivacyPolicy: React.FC = () => {
    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant="h3" component="h1" gutterBottom>
                    Privacy Policy for Storage Labels
                </Typography>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Last updated: {new Date().toLocaleDateString()}
                </Typography>

                <Box sx={{ mt: 4 }}>
                    <Typography variant="h5" gutterBottom>
                        Information We Collect
                    </Typography>
                    <Typography paragraph>
                        We collect the following information when you use Storage Labels:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Email address (from Google OAuth authentication)</li>
                        <li>Profile information (name, profile picture from your Google account)</li>
                        <li>Storage box and item data that you create and manage</li>
                        <li>Images that you upload for your items</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        How We Use Your Information
                    </Typography>
                    <Typography paragraph>
                        We use the collected information for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Authentication and authorization to access your account</li>
                        <li>Storing and managing your box and item inventory</li>
                        <li>Image storage and retrieval for your items</li>
                        <li>Providing the core functionality of the Storage Labels application</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Data Storage and Security
                    </Typography>
                    <Typography paragraph>
                        Your data security is important to us:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>All data is stored in a secure PostgreSQL database</li>
                        <li>Images are encrypted at rest using AES-256-GCM encryption</li>
                        <li>All data is transmitted over HTTPS</li>
                        <li>Hosted on secure, monitored servers</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Third-Party Services
                    </Typography>
                    <Typography paragraph>
                        We use the following third-party services:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Google OAuth for authentication</li>
                        <li>Auth0 for identity and access management</li>
                    </Box>
                    <Typography paragraph>
                        These services have their own privacy policies. We recommend reviewing them:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>
                            <Link href="https://policies.google.com/privacy" target="_blank" rel="noopener">
                                Google Privacy Policy
                            </Link>
                        </li>
                        <li>
                            <Link href="https://auth0.com/privacy" target="_blank" rel="noopener">
                                Auth0 Privacy Policy
                            </Link>
                        </li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Your Rights
                    </Typography>
                    <Typography paragraph>
                        You have the right to:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Access your personal data</li>
                        <li>Request deletion of your data</li>
                        <li>Export your data</li>
                        <li>Opt out of the service at any time</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Data Retention
                    </Typography>
                    <Typography paragraph>
                        We retain your data for as long as your account is active. If you choose to delete your account, 
                        we will delete all your personal data within 30 days, except where we are required to retain it by law.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Children's Privacy
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is not intended for use by children under the age of 13. We do not knowingly 
                        collect personal information from children under 13. If you believe we have collected information 
                        from a child under 13, please contact us immediately.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Contact Us
                    </Typography>
                    <Typography paragraph>
                        If you have any questions about this Privacy Policy, please contact us at:{' '}
                        <Link href="mailto:privacy@pollyspeople.net">privacy@pollyspeople.net</Link>
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Changes to This Policy
                    </Typography>
                    <Typography paragraph>
                        We may update this Privacy Policy from time to time. We will notify you of any changes by updating 
                        the "Last updated" date at the top of this policy. Your continued use of Storage Labels after any 
                        changes constitutes acceptance of the updated policy.
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