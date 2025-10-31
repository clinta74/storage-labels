import React, { useState, useRef, useEffect } from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    Box,
    Stack,
    IconButton,
    ToggleButtonGroup,
    ToggleButton,
    CircularProgress,
    Typography,
} from '@mui/material';
import CameraAltIcon from '@mui/icons-material/CameraAlt';
import PhotoLibraryIcon from '@mui/icons-material/PhotoLibrary';
import FlipCameraIosIcon from '@mui/icons-material/FlipCameraIos';
import CancelIcon from '@mui/icons-material/Cancel';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';

interface ImageCaptureProps {
    open: boolean;
    onClose: () => void;
    onCapture: (file: File) => void;
    uploading?: boolean;
}

type CaptureMode = 'camera' | 'file';
type CameraFacing = 'user' | 'environment';

export const ImageCapture: React.FC<ImageCaptureProps> = ({
    open,
    onClose,
    onCapture,
    uploading = false,
}) => {
    const [mode, setMode] = useState<CaptureMode>('camera');
    const [cameraFacing, setCameraFacing] = useState<CameraFacing>('environment');
    const [stream, setStream] = useState<MediaStream | null>(null);
    const [capturedImage, setCapturedImage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [hasMultipleCameras, setHasMultipleCameras] = useState(false);
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (open && mode === 'camera') {
            startCamera();
            checkMultipleCameras();
        }
        return () => {
            stopCamera();
        };
    }, [open, mode, cameraFacing]);

    const checkMultipleCameras = async () => {
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();
            const videoDevices = devices.filter(device => device.kind === 'videoinput');
            setHasMultipleCameras(videoDevices.length > 1);
        } catch (err) {
            console.error('Error checking cameras:', err);
        }
    };

    const startCamera = async () => {
        try {
            setError(null);
            const constraints: MediaStreamConstraints = {
                video: {
                    facingMode: cameraFacing,
                    width: { ideal: 1920 },
                    height: { ideal: 1080 },
                },
                audio: false,
            };

            const mediaStream = await navigator.mediaDevices.getUserMedia(constraints);
            setStream(mediaStream);

            if (videoRef.current) {
                videoRef.current.srcObject = mediaStream;
            }
        } catch (err: unknown) {
            console.error('Error accessing camera:', err);
            const errorMessage = err instanceof Error ? err.message : 'Failed to access camera';
            setError(errorMessage);
        }
    };

    const stopCamera = () => {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            setStream(null);
        }
    };

    const handleCapture = () => {
        if (!videoRef.current || !canvasRef.current) return;

        const video = videoRef.current;
        const canvas = canvasRef.current;
        const context = canvas.getContext('2d');

        if (!context) return;

        // Set canvas size to match video
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        // Draw the video frame to canvas
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        // Get the image as a data URL
        const imageDataUrl = canvas.toDataURL('image/jpeg', 0.9);
        setCapturedImage(imageDataUrl);
        stopCamera();
    };

    const handleRetake = () => {
        setCapturedImage(null);
        startCamera();
    };

    const handleConfirm = async () => {
        if (!capturedImage) return;

        // Convert data URL to File
        const response = await fetch(capturedImage);
        const blob = await response.blob();
        const file = new File([blob], `capture-${Date.now()}.jpg`, { type: 'image/jpeg' });

        onCapture(file);
        handleClose();
    };

    const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            onCapture(file);
            handleClose();
        }
    };

    const handleFlipCamera = () => {
        setCameraFacing(prev => prev === 'user' ? 'environment' : 'user');
    };

    const handleClose = () => {
        setCapturedImage(null);
        stopCamera();
        setError(null);
        onClose();
    };

    const handleModeChange = (event: React.MouseEvent<HTMLElement>, newMode: CaptureMode | null) => {
        if (newMode !== null) {
            setCapturedImage(null);
            stopCamera();
            setError(null);
            setMode(newMode);
        }
    };

    return (
        <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
            <DialogTitle>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <span>Capture Image</span>
                    <ToggleButtonGroup
                        value={mode}
                        exclusive
                        onChange={handleModeChange}
                        size="small"
                    >
                        <ToggleButton value="camera">
                            <CameraAltIcon sx={{ mr: 1 }} />
                            Camera
                        </ToggleButton>
                        <ToggleButton value="file">
                            <PhotoLibraryIcon sx={{ mr: 1 }} />
                            File
                        </ToggleButton>
                    </ToggleButtonGroup>
                </Stack>
            </DialogTitle>
            <DialogContent>
                {mode === 'camera' ? (
                    <Box>
                        {error ? (
                            <Box textAlign="center" py={4}>
                                <Typography color="error" gutterBottom>
                                    {error}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Please ensure camera permissions are granted or switch to File mode.
                                </Typography>
                            </Box>
                        ) : capturedImage ? (
                            <Box 
                                position="relative"
                                sx={{
                                    minHeight: '500px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    backgroundColor: '#000',
                                }}
                            >
                                <img
                                    src={capturedImage}
                                    alt="Captured"
                                    style={{
                                        width: '100%',
                                        height: 'auto',
                                        maxHeight: '500px',
                                        objectFit: 'contain',
                                    }}
                                />
                            </Box>
                        ) : (
                            <Box 
                                position="relative"
                                sx={{
                                    minHeight: '500px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    backgroundColor: '#000',
                                }}
                            >
                                <video
                                    ref={videoRef}
                                    autoPlay
                                    playsInline
                                    style={{
                                        width: '100%',
                                        height: 'auto',
                                        maxHeight: '500px',
                                    }}
                                />
                                {hasMultipleCameras && (
                                    <IconButton
                                        onClick={handleFlipCamera}
                                        sx={{
                                            position: 'absolute',
                                            top: 8,
                                            right: 8,
                                            backgroundColor: 'rgba(0, 0, 0, 0.5)',
                                            color: 'white',
                                            '&:hover': {
                                                backgroundColor: 'rgba(0, 0, 0, 0.7)',
                                            },
                                        }}
                                    >
                                        <FlipCameraIosIcon />
                                    </IconButton>
                                )}
                                <canvas ref={canvasRef} style={{ display: 'none' }} />
                            </Box>
                        )}
                    </Box>
                ) : (
                    <Box textAlign="center" py={4}>
                        <input
                            ref={fileInputRef}
                            accept="image/*"
                            style={{ display: 'none' }}
                            type="file"
                            onChange={handleFileSelect}
                            disabled={uploading}
                        />
                        <Button
                            variant="contained"
                            onClick={() => fileInputRef.current?.click()}
                            startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <PhotoLibraryIcon />}
                            disabled={uploading}
                            size="large"
                        >
                            {uploading ? 'Uploading...' : 'Choose Image'}
                        </Button>
                        <Typography variant="body2" color="text.secondary" mt={2}>
                            Select an image from your device
                        </Typography>
                    </Box>
                )}
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} disabled={uploading}>
                    Cancel
                </Button>
                {mode === 'camera' && !error && (
                    <>
                        {capturedImage ? (
                            <>
                                <Button
                                    onClick={handleRetake}
                                    startIcon={<CancelIcon />}
                                    disabled={uploading}
                                >
                                    Retake
                                </Button>
                                <Button
                                    onClick={handleConfirm}
                                    variant="contained"
                                    startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <CheckCircleIcon />}
                                    disabled={uploading}
                                >
                                    {uploading ? 'Uploading...' : 'Use Photo'}
                                </Button>
                            </>
                        ) : (
                            <Button
                                onClick={handleCapture}
                                variant="contained"
                                startIcon={<CameraAltIcon />}
                                disabled={!stream}
                            >
                                Take Photo
                            </Button>
                        )}
                    </>
                )}
            </DialogActions>
        </Dialog>
    );
};
