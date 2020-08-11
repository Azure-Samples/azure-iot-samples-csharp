# Device Reconnection Sample

This sample code demonstrates the various connection status changes and connection status change reasons the Device Client can return, and how to handle them.

The device client exhibits the following connection status changes with reason:

<table>
  <tr>
    <th> Connection Status </th>
    <th> Change Reason </th>
    <th> Ownership of connectivity </th>
    <th> Comments </th>
  </tr>
  <tr>
    <td> Connected </td>
    <td> Connection_Ok </td>
    <td> SDK </td>
    <td> SDK tries to remain connected to the service and can carry out all operations as normal. </td>
  </tr>
  <tr>
    <td> Disconnected_Retrying </td>
    <td> Communication_Error </td>
    <td> SDK </td>
    <td> When disconnection happens because of any reason (network failures, transient loss of connectivity etc.), SDK makes best attempt to connect back to IotHub. The RetryPolicy applied on the DeviceClient will be used to determine the count of reconnection attempts for <em>retriable</em> errors. </td>
  </tr>
  <tr>
    <td rowspan="4"> Disconnected </td>
    <td> Device_Disabled </td>
    <td rowspan="4"> Application </td>
    <td> This signifies that the device/ module has been deleted or marked as disabled (on your hub instance). <br/> Fix the device/ module status in Azure before attempting to create the associated client instance. </td>
  </tr>
  <tr>
    <td> Bad_Credential </td>
    <td> Supplied credential isn’t good for device to connect to service. <br/> Fix the supplied credentials before attempting to reconnect again </td>
  </tr>
  <tr>
    <td> Communication_Error </td>
    <td> This is the state when SDK landed up in a non-retriable error during communication. <br/> If you want to perform more operations on the device client, you should dispose and then re-initialize the client. </td>
  </tr>
  <tr>
    <td> Retry_Expired </td>
    <td> This signifies that the client was disconnected due to a transient exception, but the retry policy expired before a connection could be re-established. <br/> If you want to perform more operations on the device client, you should dispose and then re-initialize the client. </td>
  </tr>
  <tr>
    <td> Disabled </td>
    <td> Client_Close </td>
    <td> Application </td>
    <td> This is the state when SDK was asked to close the connection by application. </td>
  </tr>
</table>

NOTE:
* If the device is in `Connected` state, you can perform subsequent operations on the same client instance.
* If the device is in `Disconnected_Retrying` state, then the SDK is retrying to recover its connection. Wait until device recovers and reports a `Connected` state, and then perform subsequent operations.
* If the device is in `Disconnected` or `Disabled` state, then the underlying transport layer has been disposed. You should dispose of the existing `DeviceClient` instance and then initialize a new client (initializing a new `DeviceClient` instance without disposing the previously used instance will cause them to fight for the same connection resources).

