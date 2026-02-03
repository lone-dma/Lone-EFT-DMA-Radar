namespace eft_dma_radar.UI.Skia
{
    public enum SKWidgetClickEvent
    {
        /// <summary>
        /// No click event ocurred in this widget.
        /// </summary>
        None,
        /// <summary>
        /// An area of the Widget (including Non-Client area) was clicked.
        /// </summary>
        Clicked,
        /// <summary>
        /// The Title bar was clicked.
        /// </summary>
        ClickedTitleBar,
        /// <summary>
        /// The Client Area of the widget was clicked.
        /// </summary>
        ClickedClientArea,
        /// <summary>
        /// The Minimize Button was clicked.
        /// </summary>
        ClickedMinimize,
        /// <summary>
        /// The Resize Triangle was clicked.
        /// </summary>
        ClickedResize
    }
}
