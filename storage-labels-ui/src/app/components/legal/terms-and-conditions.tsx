import React from 'react';
import { Box, Container, Paper, Typography, Link } from '@mui/material';

export const TermsConditions: React.FC = () => {
    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant="h3" component="h1" gutterBottom>
                    Software License Agreement for Storage Labels
                </Typography>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Last updated: {new Date().toLocaleDateString()}
                </Typography>

                <Box sx={{ mt: 4 }}>
                    <Typography variant="h5" gutterBottom>
                        1. License Grant
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is open-source, self-hosted inventory management software. By installing and 
                        using this software, you agree to the terms of the applicable open-source license. This software 
                        is provided &quot;as-is&quot; for your personal or organizational use on infrastructure you control.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        2. Self-Hosted Software
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is self-hosted software that you install and run on your own infrastructure. 
                        The software provides:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Inventory management for physical storage items</li>
                        <li>Location and box organization</li>
                        <li>Image storage with encryption</li>
                        <li>Search and tracking capabilities</li>
                        <li>QR code generation</li>
                    </Box>
                    <Typography paragraph>
                        As the installer and operator of this software, you are responsible for its security, 
                        availability, and compliance with applicable laws.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        3. Your Responsibilities
                    </Typography>
                    <Typography paragraph>
                        As the installer and operator of this self-hosted software, you are responsible for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Compliance with all applicable laws and regulations in your jurisdiction</li>
                        <li>Security of your installation and infrastructure</li>
                        <li>Data backup and disaster recovery</li>
                        <li>User access management and authentication</li>
                        <li>Privacy and data protection of any users you grant access to</li>
                        <li>Proper configuration and maintenance of the software</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        4. Authentication
                    </Typography>
                    <Typography paragraph>
                        Storage Labels supports two authentication modes:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li><strong>Local Authentication:</strong> Users authenticate with username/password managed by the system</li>
                        <li><strong>No Authentication:</strong> Open access mode suitable only for trusted networks</li>
                    </Box>
                    <Typography paragraph>
                        You are responsible for choosing an appropriate authentication mode for your deployment 
                        environment and managing user access accordingly.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        5. Data Ownership
                    </Typography>
                    <Typography paragraph>
                        All data stored in your self-hosted instance of Storage Labels belongs to you. This includes:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>User accounts and profiles</li>
                        <li>Inventory data (boxes, items, locations)</li>
                        <li>Uploaded images</li>
                        <li>Configuration and preferences</li>
                    </Box>
                    <Typography paragraph>
                        As a self-hosted solution, no data is transmitted to or stored by the software developers. 
                        You maintain complete control over your data.
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
                        7. Software Updates
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is open-source software that may receive updates and improvements. However:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Updates are provided on a best-effort basis</li>
                        <li>You choose when and if to apply updates to your installation</li>
                        <li>Backward compatibility is not guaranteed between versions</li>
                        <li>You are responsible for testing updates before deploying to production</li>
                    </Box>
                    <Typography paragraph>
                        The availability and performance of your installation is your responsibility as the operator.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        8. Data Backup and Recovery
                    </Typography>
                    <Typography paragraph>
                        As the operator of this self-hosted software, you are solely responsible for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Implementing backup procedures for your database and uploaded files</li>
                        <li>Testing backup restoration regularly</li>
                        <li>Maintaining disaster recovery plans</li>
                        <li>Securing backup data appropriately</li>
                    </Box>
                    <Typography paragraph>
                        The software developers provide no backup services and are not liable for any data loss.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        9. Limitation of Liability and Warranty Disclaimer
                    </Typography>
                    <Typography paragraph>
                        This software is provided &quot;as is&quot; without warranty of any kind, either express or implied, 
                        including but not limited to warranties of merchantability, fitness for a particular purpose, 
                        or non-infringement.
                    </Typography>
                    <Typography paragraph>
                        The developers and contributors shall not be liable for any direct, indirect, incidental, 
                        special, consequential, or punitive damages, including without limitation, loss of profits, 
                        data, use, or other intangible losses, resulting from:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Your installation, configuration, or use of the software</li>
                        <li>Security vulnerabilities or unauthorized access</li>
                        <li>Data loss, corruption, or breaches</li>
                        <li>Software bugs, errors, or incompatibilities</li>
                        <li>Any other use of this self-hosted software</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        10. Indemnification
                    </Typography>
                    <Typography paragraph>
                        You agree to indemnify and hold harmless the developers, contributors, and maintainers of 
                        Storage Labels from any claims, damages, losses, liabilities, and expenses arising from:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Your installation and operation of the software</li>
                        <li>Your users&apos; access to and use of your installation</li>
                        <li>Any data breaches or security incidents</li>
                        <li>Your failure to comply with applicable laws and regulations</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        11. Open Source License
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is licensed under an open-source license. You are free to:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Use the software for personal or commercial purposes</li>
                        <li>Modify the source code to suit your needs</li>
                        <li>Distribute copies of the software</li>
                        <li>Contribute improvements back to the project</li>
                    </Box>
                    <Typography paragraph>
                        Please refer to the LICENSE file in the source repository for the complete license terms.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        12. Changes to Terms
                    </Typography>
                    <Typography paragraph>
                        These terms may be updated from time to time. Changes will be reflected in software updates. 
                        It is your responsibility to review the terms when updating the software.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        13. Governing Law
                    </Typography>
                    <Typography paragraph>
                        These terms shall be governed by and construed in accordance with applicable laws, without 
                        regard to conflict of law provisions.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        14. Support and Community
                    </Typography>
                    <Typography paragraph>
                        As self-hosted open-source software, support is community-driven. For questions, 
                        issues, or contributions:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Check the documentation in the repository</li>
                        <li>Search existing issues on GitHub</li>
                        <li>Join community discussions</li>
                        <li>Submit bug reports or feature requests</li>
                    </Box>
                    <Typography paragraph>
                        No official support is guaranteed. Community assistance is provided on a best-effort basis.
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